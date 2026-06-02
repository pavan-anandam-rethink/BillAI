using BillingService.Application.Abstractions.Messaging;
using BillingService.Contracts.Events;
using BillingService.Persistence.Legacy;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BillingService.Persistence.Outbox;

/// <summary>
/// Durable transactional outbox writer.
///
/// Persists integration events to the BillingOutboxMessages SQL table within the
/// same logical unit of work as the originating business operation. The
/// <see cref="BillingService.Workers.Outbox.OutboxPublisherWorker"/> reads pending
/// rows and publishes them to Azure Service Bus with at-least-once delivery semantics.
///
/// Design invariants:
/// - Rows are append-only. No UPDATE to the payload after insert.
/// - The publisher marks rows as Processed by setting ProcessedOnUtc.
/// - Idempotency: EventId (Guid) is unique; duplicate inserts are ignored.
/// </summary>
internal sealed class OutboxPersistenceWriter(
    IBillingSqlConnectionFactory connectionFactory,
    ILogger<OutboxPersistenceWriter> logger)
    : IOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private const string InsertSql = """
        INSERT INTO dbo.BillingOutboxMessages
            (Id, EventType, AggregateType, AggregateId, CorrelationId, Payload, CreatedOnUtc, ProcessedOnUtc)
        SELECT @Id, @EventType, @AggregateType, @AggregateId, @CorrelationId, @Payload, @CreatedOnUtc, NULL
        WHERE NOT EXISTS (SELECT 1 FROM dbo.BillingOutboxMessages WHERE Id = @Id);
        """;

    public async Task AddAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), SerializerOptions);

        try
        {
            await using var connection = connectionFactory.CreateOpenConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = InsertSql;

            command.Parameters.AddWithValue("@Id", integrationEvent.EventId);
            command.Parameters.AddWithValue("@EventType", integrationEvent.EventType);
            command.Parameters.AddWithValue("@AggregateType", integrationEvent.AggregateType);
            command.Parameters.AddWithValue("@AggregateId", integrationEvent.AggregateId);
            command.Parameters.AddWithValue("@CorrelationId",
                (object?)integrationEvent.CorrelationId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Payload", payload);
            command.Parameters.AddWithValue("@CreatedOnUtc", integrationEvent.OccurredOnUtc.UtcDateTime);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            if (rowsAffected == 0)
            {
                logger.LogWarning(
                    "BillingService outbox: duplicate event ignored. EventId={EventId} EventType={EventType}",
                    integrationEvent.EventId,
                    integrationEvent.EventType);
            }
            else
            {
                logger.LogDebug(
                    "BillingService outbox: persisted event. EventId={EventId} EventType={EventType}",
                    integrationEvent.EventId,
                    integrationEvent.EventType);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex,
                "BillingService outbox: failed to persist event. EventId={EventId} EventType={EventType}",
                integrationEvent.EventId,
                integrationEvent.EventType);

            throw;
        }
    }
}
