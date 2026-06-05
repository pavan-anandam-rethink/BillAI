using BillingService.Application.Abstractions.Messaging;
using BillingService.Contracts.Events;
using BillingService.Persistence.Legacy;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace BillingService.Persistence.Outbox;

public sealed class OutboxPersistenceWriter(IBillingSqlConnectionFactory connectionFactory) : IOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task AddAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        using var command = new SqlCommand(@"
            INSERT INTO billing.BillingOutbox
                (Id, EventType, Payload, CorrelationId, OccurredOnUtc, AttemptCount)
            VALUES
                (@Id, @EventType, @Payload, @CorrelationId, @OccurredOnUtc, 0)", connection);

        command.Parameters.AddWithValue("@Id", integrationEvent.EventId);
        command.Parameters.AddWithValue("@EventType", integrationEvent.EventType);
        command.Parameters.AddWithValue("@Payload",
            JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), SerializerOptions));
        command.Parameters.AddWithValue("@CorrelationId",
            (object?)integrationEvent.CorrelationId ?? DBNull.Value);
        command.Parameters.AddWithValue("@OccurredOnUtc", integrationEvent.OccurredOnUtc);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
