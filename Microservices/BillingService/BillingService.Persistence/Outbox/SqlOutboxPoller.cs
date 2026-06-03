using BillingService.Application.Abstractions.Messaging;
using BillingService.Persistence.Legacy;
using Microsoft.Data.SqlClient;

namespace BillingService.Persistence.Outbox;

/// <summary>
/// Polls the durable outbox table for pending messages and records processing outcomes.
/// Used by <see cref="BillingService.Workers.Outbox.OutboxPublisherWorker"/> on a timed loop.
/// </summary>
public sealed class SqlOutboxPoller(IBillingSqlConnectionFactory connectionFactory) : IOutboxPoller
{
    private const string FetchSql = """
        SELECT TOP (@BatchSize)
            Id, EventType, Payload, CorrelationId, OccurredOnUtc, AttemptCount
        FROM [billing].[OutboxMessages]
        WHERE ProcessedOnUtc IS NULL
          AND (ProcessingError IS NULL OR AttemptCount < 5)
        ORDER BY OccurredOnUtc ASC
        """;

    private const string MarkProcessedSql = """
        UPDATE [billing].[OutboxMessages]
        SET ProcessedOnUtc = @ProcessedOnUtc
        WHERE Id = @Id
        """;

    private const string RecordFailureSql = """
        UPDATE [billing].[OutboxMessages]
        SET ProcessingError = @Error,
            AttemptCount    = AttemptCount + 1
        WHERE Id = @Id
        """;

    public async Task<IReadOnlyList<OutboxMessage>> FetchPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(FetchSql, connection);
        command.Parameters.AddWithValue("@BatchSize", batchSize);

        var messages = new List<OutboxMessage>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            messages.Add(new OutboxMessage
            {
                Id = reader.GetGuid(0),
                EventType = reader.GetString(1),
                Payload = reader.GetString(2),
                CorrelationId = reader.IsDBNull(3) ? null : reader.GetString(3),
                OccurredOnUtc = reader.GetFieldValue<DateTimeOffset>(4),
                AttemptCount = reader.GetInt32(5)
            });
        }

        return messages;
    }

    public async Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(MarkProcessedSql, connection);
        command.Parameters.AddWithValue("@ProcessedOnUtc", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("@Id", messageId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordFailureAsync(
        Guid messageId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(RecordFailureSql, connection);
        command.Parameters.AddWithValue("@Error", errorMessage);
        command.Parameters.AddWithValue("@Id", messageId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
