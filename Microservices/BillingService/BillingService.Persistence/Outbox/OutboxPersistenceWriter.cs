using BillingService.Application.Abstractions.Messaging;
using BillingService.Contracts.Events;
using BillingService.Persistence.Legacy;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace BillingService.Persistence.Outbox;

/// <summary>
/// Writes integration events to the durable outbox table inside the same SQL transaction
/// as the business write, guaranteeing at-least-once delivery without distributed transactions.
/// </summary>
public sealed class OutboxPersistenceWriter(IBillingSqlConnectionFactory connectionFactory) : IOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private const string InsertSql = """
        INSERT INTO [billing].[OutboxMessages]
            (Id, EventType, Payload, CorrelationId, OccurredOnUtc, AttemptCount)
        VALUES
            (@Id, @EventType, @Payload, @CorrelationId, @OccurredOnUtc, 0)
        """;

    public async Task AddAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(InsertSql, connection);

        command.Parameters.AddWithValue("@Id", integrationEvent.EventId);
        command.Parameters.AddWithValue("@EventType", integrationEvent.EventType);
        command.Parameters.AddWithValue(
            "@Payload",
            JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), SerializerOptions));
        command.Parameters.AddWithValue("@CorrelationId", (object?)integrationEvent.CorrelationId ?? DBNull.Value);
        command.Parameters.AddWithValue("@OccurredOnUtc", integrationEvent.OccurredOnUtc);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
