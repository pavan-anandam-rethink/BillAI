using BillingService.Application.Abstractions.Messaging;
using BillingService.Contracts.Events;
using BillingService.Persistence.Legacy;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BillingService.Persistence.Outbox;

/// <summary>
/// Writes integration events to the durable outbox table in the same transaction as the business write.
/// The outbox table must exist in the BillingDb before this writer is enabled.
/// Schema: BillingOutbox (Id, EventType, Payload, CorrelationId, OccurredOnUtc, ProcessedOnUtc, ProcessingError, AttemptCount).
/// </summary>
public sealed class OutboxPersistenceWriter(
    IBillingSqlConnectionFactory connectionFactory,
    ILogger<OutboxPersistenceWriter> logger)
    : IOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private const string InsertSql = @"
        INSERT INTO dbo.BillingOutbox
            (Id, EventType, Payload, CorrelationId, OccurredOnUtc, AttemptCount)
        VALUES
            (@Id, @EventType, @Payload, @CorrelationId, @OccurredOnUtc, 0);";

    public async Task AddAsync(
        IntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var message = new OutboxMessage
        {
            Id = integrationEvent.EventId,
            EventType = integrationEvent.EventType,
            Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), SerializerOptions),
            CorrelationId = integrationEvent.CorrelationId,
            OccurredOnUtc = integrationEvent.OccurredOnUtc
        };

        // CreateOpenConnection() returns an already-opened connection.
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(InsertSql, connection);
        command.Parameters.AddWithValue("@Id", message.Id);
        command.Parameters.AddWithValue("@EventType", message.EventType);
        command.Parameters.AddWithValue("@Payload", message.Payload);
        command.Parameters.AddWithValue("@CorrelationId", (object?)message.CorrelationId ?? DBNull.Value);
        command.Parameters.AddWithValue("@OccurredOnUtc", message.OccurredOnUtc);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Wrote outbox message {MessageId} for event type {EventType}",
            message.Id, message.EventType);
    }
}
