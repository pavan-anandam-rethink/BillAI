using BillingService.Application.Abstractions.Messaging;
using BillingService.Persistence.Legacy;
using Microsoft.Data.SqlClient;

namespace BillingService.Persistence.Outbox;

/// <summary>
/// SQL Server implementation of <see cref="IOutboxPoller"/>.
/// Reads unprocessed outbox messages ordered by <c>OccurredOnUtc</c> (FIFO),
/// marks them processed or failed after the caller attempts delivery.
/// The poll, mark-processed, and mark-failed operations are idempotent:
/// re-running them against an already-processed row is a no-op.
/// </summary>
public sealed class SqlOutboxPoller(IBillingSqlConnectionFactory connectionFactory)
    : IOutboxPoller
{
    private const int MaxAttempts = 5;

    public async Task<IReadOnlyList<OutboxMessage>> FetchPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        const string sql = """
            SELECT TOP (@BatchSize)
                Id,
                EventType,
                Payload,
                CorrelationId,
                OccurredOnUtc,
                ProcessedOnUtc,
                ProcessingError,
                AttemptCount
            FROM billing.OutboxMessages
            WHERE ProcessedOnUtc IS NULL
              AND AttemptCount < @MaxAttempts
            ORDER BY OccurredOnUtc
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BatchSize", batchSize);
        command.Parameters.AddWithValue("@MaxAttempts", MaxAttempts);

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
                OccurredOnUtc = reader.GetDateTimeOffset(4),
                ProcessedOnUtc = reader.IsDBNull(5) ? null : reader.GetDateTimeOffset(5),
                ProcessingError = reader.IsDBNull(6) ? null : reader.GetString(6),
                AttemptCount = reader.GetInt32(7)
            });
        }

        return messages;
    }

    public async Task MarkProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        const string sql = """
            UPDATE billing.OutboxMessages
            SET ProcessedOnUtc = @ProcessedOnUtc,
                ProcessingError = NULL
            WHERE Id = @Id
              AND ProcessedOnUtc IS NULL
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", messageId);
        command.Parameters.AddWithValue("@ProcessedOnUtc", DateTimeOffset.UtcNow);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        const string sql = """
            UPDATE billing.OutboxMessages
            SET AttemptCount = AttemptCount + 1,
                ProcessingError = @Error
            WHERE Id = @Id
              AND ProcessedOnUtc IS NULL
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", messageId);
        command.Parameters.AddWithValue("@Error", error.Length > 2000 ? error[..2000] : error);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
