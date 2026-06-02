using BillingService.Application.Abstractions.Messaging;
using BillingService.Contracts.Events;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BillingService.Workers.Outbox;

/// <summary>
/// Background worker that polls the <c>BillingOutboxMessage</c> SQL table and
/// publishes unpublished integration events to Azure Service Bus.
///
/// Design principles:
/// <list type="bullet">
///   <item>At-least-once delivery: events are published before being marked processed.</item>
///   <item>Idempotency: each event carries a stable <c>Id</c> used as the Service Bus MessageId,
///         so duplicate deliveries are safe for idempotent consumers.</item>
///   <item>Dead-letter threshold: events exceeding <see cref="MaxAttempts"/> are marked failed
///         and skipped to avoid blocking the queue.</item>
///   <item>Enabled only when <c>BillingService:Modernization:EnableOutboxPublisher</c> = true.</item>
/// </list>
/// </summary>
public sealed class OutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisherWorker> logger)
    : BackgroundService
{
    private const int MaxAttempts = 5;
    private const int BatchSize = 20;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
                logger.LogError(ex, "Outbox publisher batch failed — will retry after {Interval}.", PollingInterval);
            }

            await Task.Delay(PollingInterval, stoppingToken).ConfigureAwait(false);
        }

        logger.LogInformation("BillingService outbox publisher worker stopped.");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var sqlFactory = scope.ServiceProvider
            .GetService<BillingService.Persistence.Legacy.IBillingSqlConnectionFactory>();

        if (sqlFactory is null)
        {
            // Persistence compatibility layer not registered — skip.
            return;
        }

        var eventBus = scope.ServiceProvider.GetService<IEventBus>();
        if (eventBus is null)
        {
            return;
        }

        await using var connection = new SqlConnection(
            sqlFactory.CreateOpenConnection().ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var messages = await ReadPendingMessagesAsync(connection, cancellationToken)
            .ConfigureAwait(false);

        if (messages.Count == 0)
        {
            return;
        }

        logger.LogInformation("Outbox: processing {Count} pending message(s).", messages.Count);

        foreach (var message in messages)
        {
            await PublishMessageAsync(message, eventBus, connection, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async Task<List<OutboxMessage>> ReadPendingMessagesAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (@batchSize) [Id], [EventType], [Payload], [CorrelationId], [OccurredOnUtc], [AttemptCount]
            FROM [dbo].[BillingOutboxMessage]
            WHERE [ProcessedOnUtc] IS NULL
              AND [AttemptCount] < @maxAttempts
            ORDER BY [OccurredOnUtc] ASC
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@batchSize", BatchSize);
        command.Parameters.AddWithValue("@maxAttempts", MaxAttempts);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        var messages = new List<OutboxMessage>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            messages.Add(new OutboxMessage
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                EventType = reader.GetString(reader.GetOrdinal("EventType")),
                Payload = reader.GetString(reader.GetOrdinal("Payload")),
                CorrelationId = reader.IsDBNull(reader.GetOrdinal("CorrelationId"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("CorrelationId")),
                OccurredOnUtc = reader.GetDateTimeOffset(reader.GetOrdinal("OccurredOnUtc")),
                AttemptCount = reader.GetInt32(reader.GetOrdinal("AttemptCount"))
            });
        }

        return messages;
    }

    private async Task PublishMessageAsync(
        OutboxMessage message,
        IEventBus eventBus,
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            var integrationEvent = DeserializeEvent(message);
            if (integrationEvent is null)
            {
                await MarkFailedAsync(connection, message.Id,
                    $"Cannot deserialize event type '{message.EventType}'.", cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            await eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
            await MarkProcessedAsync(connection, message.Id, cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Outbox: published {EventType} id={Id} correlationId={CorrelationId}",
                message.EventType, message.Id, message.CorrelationId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Outbox: failed to publish {EventType} id={Id} attempt={Attempt}",
                message.EventType, message.Id, message.AttemptCount + 1);

            await IncrementAttemptAsync(connection, message.Id, ex.Message, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static IntegrationEvent? DeserializeEvent(OutboxMessage message)
    {
        // Known event types are deserialized to concrete types for type-safe routing.
        // As new event types are added, extend this switch.
        return message.EventType switch
        {
            "billing.operation.completed" =>
                JsonSerializer.Deserialize<BillingService.Contracts.Events.BillingOperationCompletedEvent>(
                    message.Payload, JsonOptions),
            _ => null
        };
    }

    private static async Task MarkProcessedAsync(
        SqlConnection connection, Guid id, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE [dbo].[BillingOutboxMessage]
            SET [ProcessedOnUtc] = SYSDATETIMEOFFSET(), [AttemptCount] = [AttemptCount] + 1
            WHERE [Id] = @id
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task IncrementAttemptAsync(
        SqlConnection connection, Guid id, string error, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE [dbo].[BillingOutboxMessage]
            SET [AttemptCount] = [AttemptCount] + 1,
                [ProcessingError] = @error
            WHERE [Id] = @id
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@error",
            error.Length > 2000 ? error[..2000] : error);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task MarkFailedAsync(
        SqlConnection connection, Guid id, string error, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE [dbo].[BillingOutboxMessage]
            SET [ProcessedOnUtc] = SYSDATETIMEOFFSET(),
                [AttemptCount] = @maxAttempts,
                [ProcessingError] = @error
            WHERE [Id] = @id
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@maxAttempts", MaxAttempts);
        command.Parameters.AddWithValue("@error",
            error.Length > 2000 ? error[..2000] : error);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
