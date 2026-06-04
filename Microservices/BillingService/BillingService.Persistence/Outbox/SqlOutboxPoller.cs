using BillingService.Application.Abstractions.Messaging;
using BillingService.Persistence.Legacy;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BillingService.Persistence.Outbox;

/// <summary>
/// Polls the billing.BillingOutbox table for unprocessed messages
/// and provides lifecycle management for the outbox relay loop.
/// Messages that exceed <see cref="MaxAttemptCount"/> are skipped permanently.
/// </summary>
public sealed class SqlOutboxPoller(
    IBillingSqlConnectionFactory connectionFactory,
    ILogger<SqlOutboxPoller> logger)
    : IOutboxPoller
{
    private const int MaxAttemptCount = 5;

    private const string PollSql =
        """
        SELECT TOP (@BatchSize)
            Id, EventType, Payload, CorrelationId,
            OccurredOnUtc, ProcessedOnUtc, ProcessingError, AttemptCount
        FROM billing.BillingOutbox WITH (UPDLOCK, READPAST)
        WHERE ProcessedOnUtc IS NULL
          AND AttemptCount < @MaxAttemptCount
        ORDER BY OccurredOnUtc ASC;
        """;

    private const string MarkProcessedSql =
        """
        UPDATE billing.BillingOutbox
        SET ProcessedOnUtc = SYSUTCDATETIME()
        WHERE Id IN (SELECT value FROM STRING_SPLIT(@Ids, ','));
        """;

    private const string RecordFailureSql =
        """
        UPDATE billing.BillingOutbox
        SET AttemptCount   = AttemptCount + 1,
            ProcessingError = @Error
        WHERE Id = @Id;
        """;

    public async Task<IReadOnlyList<OutboxMessage>> PollAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be positive.");
        }

        var results = new List<OutboxMessage>();

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(PollSql, connection);
        command.Parameters.Add("@BatchSize", SqlDbType.Int).Value = batchSize;
        command.Parameters.Add("@MaxAttemptCount", SqlDbType.Int).Value = MaxAttemptCount;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(new OutboxMessage
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

        logger.LogDebug("Outbox poll returned {Count} messages", results.Count);
        return results;
    }

    public async Task MarkProcessedAsync(
        IEnumerable<Guid> messageIds,
        CancellationToken cancellationToken = default)
    {
        var ids = string.Join(',', messageIds.Select(id => id.ToString("D")));
        if (string.IsNullOrWhiteSpace(ids))
        {
            return;
        }

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(MarkProcessedSql, connection);
        command.Parameters.Add("@Ids", SqlDbType.NVarChar, -1).Value = ids;

        var updated = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Marked {Count} outbox messages as processed", updated);
    }

    public async Task RecordFailureAsync(
        Guid messageId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(RecordFailureSql, connection);
        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = messageId;
        command.Parameters.Add("@Error", SqlDbType.NVarChar, 2000).Value = errorMessage;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        logger.LogWarning(
            "Recorded outbox failure for message {MessageId}: {Error}",
            messageId,
            errorMessage);
    }
}
