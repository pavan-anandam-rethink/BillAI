using BillingService.Application.Abstractions.Messaging;
using BillingService.Contracts.Events;
using BillingService.Persistence.Legacy;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace BillingService.Persistence.Outbox;

/// <summary>
/// Persists integration events to the durable outbox table inside the same
/// SQL transaction as the originating business operation.
/// Uses raw ADO.NET so the write can share the caller's connection string
/// without requiring a full EF context dependency.
/// </summary>
public sealed class OutboxPersistenceWriter(IBillingSqlConnectionFactory connectionFactory)
    : IOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task AddAsync(
        IntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = integrationEvent.EventType,
            Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), SerializerOptions),
            CorrelationId = integrationEvent.CorrelationId,
            OccurredOnUtc = DateTimeOffset.UtcNow,
            AttemptCount = 0
        };

        using var connection = connectionFactory.CreateOpenConnection();

        const string sql = """
            INSERT INTO billing.OutboxMessages
                (Id, EventType, Payload, CorrelationId, OccurredOnUtc, AttemptCount)
            VALUES
                (@Id, @EventType, @Payload, @CorrelationId, @OccurredOnUtc, @AttemptCount)
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", message.Id);
        command.Parameters.AddWithValue("@EventType", message.EventType);
        command.Parameters.AddWithValue("@Payload", message.Payload);
        command.Parameters.AddWithValue("@CorrelationId", (object?)message.CorrelationId ?? DBNull.Value);
        command.Parameters.AddWithValue("@OccurredOnUtc", message.OccurredOnUtc);
        command.Parameters.AddWithValue("@AttemptCount", message.AttemptCount);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
