using BillingService.Application.Abstractions.Messaging;
using BillingService.Contracts.Events;
using BillingService.Persistence.Legacy;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BillingService.Workers.Outbox;

/// <summary>
/// Background worker that polls the durable BillingOutbox table and publishes
/// unprocessed integration events to Azure Service Bus.
/// Guarantees at-least-once delivery; consumers must be idempotent using
/// the EventId as the deduplication key.
/// </summary>
public sealed class OutboxPublisherWorker(
    IServiceProvider serviceProvider,
    ILogger<OutboxPublisherWorker> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private const int BatchSize = 50;
    private const int MaxRetryCount = 5;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);

    private const string FetchSql = $"""
        SELECT TOP ({BatchSize})
            Id,
            EventId,
            EventType,
            AggregateType,
            AggregateId,
            CorrelationId,
            Payload,
            SchemaVersion,
            RetryCount
        FROM dbo.BillingOutbox WITH (READPAST)
        WHERE ProcessedOnUtc IS NULL
          AND RetryCount < {MaxRetryCount}
        ORDER BY CreatedOnUtc
        """;

    private const string MarkProcessedSql = """
        UPDATE dbo.BillingOutbox
        SET ProcessedOnUtc = SYSUTCDATETIME()
        WHERE Id = @Id
        """;

    private const string MarkFailedSql = """
        UPDATE dbo.BillingOutbox
        SET RetryCount = RetryCount + 1,
            Error      = @Error
        WHERE Id = @Id
        """;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BillingService outbox publisher worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox publisher encountered an unexpected error during batch processing.");
            }

            await Task.Delay(PollingInterval, stoppingToken).ConfigureAwait(false);
        }

        logger.LogInformation("BillingService outbox publisher worker stopped.");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var connectionFactory = scope.ServiceProvider.GetRequiredService<IBillingSqlConnectionFactory>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await FetchPendingRowsAsync(connection, cancellationToken).ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return;
        }

        logger.LogInformation("Outbox publisher processing batch of {Count} events.", rows.Count);

        foreach (var row in rows)
        {
            await ProcessRowAsync(row, connection, eventBus, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<List<OutboxRow>> FetchPendingRowsAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        var rows = new List<OutboxRow>();

        await using var command = new SqlCommand(FetchSql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(new OutboxRow(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetString(6),
                reader.GetInt32(7),
                reader.GetInt32(8)));
        }

        return rows;
    }

    private async Task ProcessRowAsync(
        OutboxRow row,
        SqlConnection connection,
        IEventBus eventBus,
        CancellationToken cancellationToken)
    {
        try
        {
            var integrationEvent = DeserializeEvent(row);
            if (integrationEvent is null)
            {
                logger.LogWarning(
                    "Outbox row {RowId} has unknown EventType '{EventType}'; skipping.",
                    row.Id, row.EventType);
                await MarkFailedAsync(connection, row.Id, "Unknown EventType; cannot deserialize.", cancellationToken).ConfigureAwait(false);
                return;
            }

            await eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
            await MarkProcessedAsync(connection, row.Id, cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Outbox published event {EventType} (EventId={EventId} CorrelationId={CorrelationId})",
                row.EventType, row.EventId, row.CorrelationId ?? "none");
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to publish outbox event {EventType} (RowId={RowId} RetryCount={RetryCount})",
                row.EventType, row.Id, row.RetryCount);

            await MarkFailedAsync(connection, row.Id, ex.Message, cancellationToken).ConfigureAwait(false);
        }
    }

    private static IntegrationEvent? DeserializeEvent(OutboxRow row)
    {
        // Resolve concrete type from the event type discriminator.
        // Add additional event types here as the domain expands.
        return row.EventType switch
        {
            "billing.operation.completed" =>
                JsonSerializer.Deserialize<BillingOperationCompletedEvent>(row.Payload, SerializerOptions),
            _ => null
        };
    }

    private static async Task MarkProcessedAsync(
        SqlConnection connection,
        Guid rowId,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(MarkProcessedSql, connection);
        command.Parameters.AddWithValue("@Id", rowId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task MarkFailedAsync(
        SqlConnection connection,
        Guid rowId,
        string error,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(MarkFailedSql, connection);
        command.Parameters.AddWithValue("@Id", rowId);
        command.Parameters.AddWithValue("@Error", error.Length > 2048 ? error[..2048] : error);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private sealed record OutboxRow(
        Guid Id,
        Guid EventId,
        string EventType,
        string AggregateType,
        string AggregateId,
        string? CorrelationId,
        string Payload,
        int SchemaVersion,
        int RetryCount);
}
