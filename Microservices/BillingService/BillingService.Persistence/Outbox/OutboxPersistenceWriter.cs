using BillingService.Application.Abstractions.Messaging;
using BillingService.Contracts.Events;
using BillingService.Persistence.Legacy;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BillingService.Persistence.Outbox;

/// <summary>
/// Persists integration events to the durable billing.OutboxMessages SQL table.
/// The write is intended to occur within the same database transaction as the business operation,
/// guaranteeing at-least-once delivery through the outbox pattern.
/// </summary>
internal sealed class OutboxPersistenceWriter(
    IBillingSqlConnectionFactory connectionFactory,
    ILogger<OutboxPersistenceWriter> logger)
    : IOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private const string InsertSql = """
        INSERT INTO [billing].[OutboxMessages]
            ([Id], [EventType], [Payload], [CorrelationId], [OccurredOnUtc], [AttemptCount])
        VALUES
            (@Id, @EventType, @Payload, @CorrelationId, @OccurredOnUtc, 0);
        """;

    public async Task AddAsync(
        IntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), SerializerOptions);

        using var connection = connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = InsertSql;

        command.Parameters.AddWithValue("@Id", integrationEvent.EventId);
        command.Parameters.AddWithValue("@EventType", integrationEvent.EventType);
        command.Parameters.AddWithValue("@Payload", payload);
        command.Parameters.AddWithValue("@CorrelationId",
            (object?)integrationEvent.CorrelationId ?? DBNull.Value);
        command.Parameters.AddWithValue("@OccurredOnUtc", integrationEvent.OccurredOnUtc);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        logger.LogDebug(
            "Persisted outbox message {EventId} of type {EventType}",
            integrationEvent.EventId,
            integrationEvent.EventType);
    }
}
