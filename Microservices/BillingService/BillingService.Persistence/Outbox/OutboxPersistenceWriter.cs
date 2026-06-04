using BillingService.Application.Abstractions.Messaging;
using BillingService.Contracts.Events;
using BillingService.Persistence.Legacy;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json;

namespace BillingService.Persistence.Outbox;

/// <summary>
/// Writes integration events to the durable billing.BillingOutbox table
/// as part of the same logical business transaction.
/// Idempotent: duplicate EventId rows are silently ignored.
/// </summary>
public sealed class OutboxPersistenceWriter(
    IBillingSqlConnectionFactory connectionFactory,
    ILogger<OutboxPersistenceWriter> logger)
    : IOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private const string InsertSql =
        """
        INSERT INTO billing.BillingOutbox
            (Id, EventType, Payload, CorrelationId, OccurredOnUtc, AttemptCount)
        SELECT @Id, @EventType, @Payload, @CorrelationId, @OccurredOnUtc, 0
        WHERE NOT EXISTS (
            SELECT 1 FROM billing.BillingOutbox WHERE Id = @Id
        );
        """;

    public async Task AddAsync(
        IntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var payload = JsonSerializer.Serialize(
            integrationEvent,
            integrationEvent.GetType(),
            SerializerOptions);

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(InsertSql, connection);
        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = integrationEvent.EventId;
        command.Parameters.Add("@EventType", SqlDbType.NVarChar, 256).Value = integrationEvent.EventType;
        command.Parameters.Add("@Payload", SqlDbType.NVarChar, -1).Value = payload;
        command.Parameters.Add("@CorrelationId", SqlDbType.NVarChar, 128).Value =
            (object?)integrationEvent.CorrelationId ?? DBNull.Value;
        command.Parameters.Add("@OccurredOnUtc", SqlDbType.DateTimeOffset).Value = integrationEvent.OccurredOnUtc;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Wrote outbox event {EventType} id {EventId} correlationId {CorrelationId}",
            integrationEvent.EventType,
            integrationEvent.EventId,
            integrationEvent.CorrelationId);
    }
}
