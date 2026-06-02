using BillingService.Application.Abstractions.Messaging;
using BillingService.Contracts.Events;
using BillingService.Persistence.Legacy;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace BillingService.Persistence.Outbox;

/// <summary>
/// Persists an <see cref="IntegrationEvent"/> to the durable outbox table (dbo.BillingOutbox)
/// within the current ambient database connection. The write must be part of the same ADO.NET
/// or EF Core transaction as the triggering business write so the outbox entry is committed
/// atomically with the business state change.
/// </summary>
public sealed class SqlOutboxWriter(IBillingSqlConnectionFactory connectionFactory) : IOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private const string InsertSql =
        """
        INSERT INTO dbo.BillingOutbox
               (Id, EventType, Payload, CorrelationId, OccurredOnUtc, AttemptCount)
        VALUES (@Id, @EventType, @Payload, @CorrelationId, @OccurredOnUtc, 0)
        """;

    public async Task AddAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), SerializerOptions);

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(InsertSql, connection);

        command.Parameters.AddWithValue("@Id", integrationEvent.EventId);
        command.Parameters.AddWithValue("@EventType", integrationEvent.EventType);
        command.Parameters.AddWithValue("@Payload", payload);
        command.Parameters.AddWithValue("@CorrelationId", (object?)integrationEvent.CorrelationId ?? DBNull.Value);
        command.Parameters.AddWithValue("@OccurredOnUtc", integrationEvent.OccurredOnUtc);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
