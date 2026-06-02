using BillingService.Application.Abstractions.Messaging;
using BillingService.Contracts.Events;
using BillingService.Persistence.Legacy;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace BillingService.Persistence.Outbox;

/// <summary>
/// Writes integration events to the durable BillingOutbox table within the
/// same SQL connection as the caller's business transaction, guaranteeing
/// exactly-once event persistence via the transactional outbox pattern.
/// </summary>
public sealed class BillingOutboxWriter(IBillingSqlConnectionFactory connectionFactory) : IOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private const string InsertSql = """
        INSERT INTO dbo.BillingOutbox
            (EventId, EventType, AggregateType, AggregateId, CorrelationId, Payload, SchemaVersion, CreatedOnUtc)
        VALUES
            (@EventId, @EventType, @AggregateType, @AggregateId, @CorrelationId, @Payload, @SchemaVersion, SYSUTCDATETIME())
        """;

    public async Task AddAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), SerializerOptions);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = new SqlCommand(InsertSql, connection);
        command.Parameters.AddWithValue("@EventId", integrationEvent.EventId);
        command.Parameters.AddWithValue("@EventType", integrationEvent.EventType);
        command.Parameters.AddWithValue("@AggregateType", integrationEvent.AggregateType);
        command.Parameters.AddWithValue("@AggregateId", integrationEvent.AggregateId);
        command.Parameters.AddWithValue("@CorrelationId", (object?)integrationEvent.CorrelationId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Payload", payload);
        command.Parameters.AddWithValue("@SchemaVersion", integrationEvent.SchemaVersion);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
