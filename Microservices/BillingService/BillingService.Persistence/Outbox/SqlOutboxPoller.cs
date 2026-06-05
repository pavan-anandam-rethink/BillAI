using BillingService.Application.Abstractions.Messaging;
using BillingService.Persistence.Legacy;
using Microsoft.Data.SqlClient;

namespace BillingService.Persistence.Outbox;

public sealed class SqlOutboxPoller(IBillingSqlConnectionFactory connectionFactory) : IOutboxPoller
{
    public async Task<IReadOnlyList<OutboxMessage>> PollAsync(
        int batchSize = 20,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        using var command = new SqlCommand(@"
            SELECT TOP (@BatchSize)
                Id, EventType, Payload, CorrelationId, OccurredOnUtc, AttemptCount
            FROM billing.BillingOutbox
            WHERE ProcessedOnUtc IS NULL
            ORDER BY OccurredOnUtc ASC", connection);

        command.Parameters.AddWithValue("@BatchSize", batchSize);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var messages = new List<OutboxMessage>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            messages.Add(new OutboxMessage
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                EventType = reader.GetString(reader.GetOrdinal("EventType")),
                Payload = reader.GetString(reader.GetOrdinal("Payload")),
                CorrelationId = reader.IsDBNull(reader.GetOrdinal("CorrelationId"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("CorrelationId")),
                OccurredOnUtc = reader.GetDateTimeOffset(reader.GetOrdinal("OccurredOnUtc")),
                AttemptCount = reader.GetInt32(reader.GetOrdinal("AttemptCount"))
            });
        }

        return messages;
    }

    public async Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        using var command = new SqlCommand(@"
            UPDATE billing.BillingOutbox
            SET ProcessedOnUtc = SYSUTCDATETIME()
            WHERE Id = @Id", connection);

        command.Parameters.AddWithValue("@Id", messageId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        using var command = new SqlCommand(@"
            UPDATE billing.BillingOutbox
            SET AttemptCount = AttemptCount + 1,
                ProcessingError = @Error
            WHERE Id = @Id", connection);

        command.Parameters.AddWithValue("@Id", messageId);
        command.Parameters.AddWithValue("@Error", error);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
