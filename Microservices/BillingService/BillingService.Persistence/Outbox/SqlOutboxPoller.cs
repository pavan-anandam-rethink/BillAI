using BillingService.Application.Abstractions.Messaging;
using BillingService.Persistence.Legacy;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace BillingService.Persistence.Outbox;

/// <summary>
/// SQL Server implementation of <see cref="IOutboxPoller"/>.
/// Reads pending outbox rows, marks them as processed, and records failures.
/// Designed to be resolved per-batch inside a scoped <see cref="IServiceScope"/>
/// by the <c>OutboxPublisherWorker</c>.
/// </summary>
public sealed class SqlOutboxPoller(
    IBillingSqlConnectionFactory connectionFactory,
    ILogger<SqlOutboxPoller> logger)
    : IOutboxPoller
{
    public async Task<IReadOnlyList<OutboxMessage>> FetchPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be positive.");
        }

        const string sql = """
            SELECT TOP (@BatchSize)
                Id, EventType, Payload, CorrelationId, OccurredOnUtc,
                ProcessedOnUtc, ProcessingError, AttemptCount
            FROM billing.BillingOutbox
            WHERE ProcessedOnUtc IS NULL
              AND AttemptCount < 5
            ORDER BY OccurredOnUtc ASC
            """;

        var messages = new List<OutboxMessage>(batchSize);

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BatchSize", batchSize);

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

        logger.LogDebug("Fetched {Count} pending outbox messages", messages.Count);
        return messages;
    }

    public async Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE billing.BillingOutbox
            SET ProcessedOnUtc = @ProcessedOnUtc
            WHERE Id = @Id
            """;

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", messageId);
        command.Parameters.AddWithValue("@ProcessedOnUtc", DateTimeOffset.UtcNow);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Marked outbox message {MessageId} as processed", messageId);
    }

    public async Task RecordFailureAsync(
        Guid messageId,
        string errorReason,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE billing.BillingOutbox
            SET ProcessingError = @Error,
                AttemptCount    = AttemptCount + 1
            WHERE Id = @Id
            """;

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", messageId);
        command.Parameters.AddWithValue("@Error", errorReason.Length > 2000
            ? errorReason[..2000]
            : errorReason);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        logger.LogWarning(
            "Recorded delivery failure for outbox message {MessageId}: {Error}",
            messageId, errorReason);
    }
}
