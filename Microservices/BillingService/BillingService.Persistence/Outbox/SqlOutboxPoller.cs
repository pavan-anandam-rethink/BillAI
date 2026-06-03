using BillingService.Application.Abstractions.Messaging;
using BillingService.Persistence.Legacy;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace BillingService.Persistence.Outbox;

/// <summary>
/// Reads unprocessed outbox messages from the durable BillingOutbox table and
/// marks them processed or failed after delivery attempts.
/// Used by OutboxPublisherWorker via IServiceScopeFactory (one scope per batch).
/// </summary>
public sealed class SqlOutboxPoller(
    IBillingSqlConnectionFactory connectionFactory,
    ILogger<SqlOutboxPoller> logger)
    : IOutboxPoller
{
    private const string FetchSql = @"
        SELECT TOP (@BatchSize) Id, EventType, Payload, CorrelationId, OccurredOnUtc, AttemptCount
        FROM   dbo.BillingOutbox WITH (READPAST)
        WHERE  ProcessedOnUtc IS NULL
          AND  AttemptCount   < 5
        ORDER  BY OccurredOnUtc ASC;";

    private const string MarkProcessedSql = @"
        UPDATE dbo.BillingOutbox
        SET    ProcessedOnUtc = @ProcessedOnUtc
        WHERE  Id = @Id;";

    private const string MarkFailedSql = @"
        UPDATE dbo.BillingOutbox
        SET    AttemptCount   = AttemptCount + 1,
               ProcessingError = @Error
        WHERE  Id = @Id;";

    public async Task<IReadOnlyList<OutboxMessage>> FetchPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var results = new List<OutboxMessage>();

        // CreateOpenConnection() returns an already-opened connection.
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(FetchSql, connection);
        command.Parameters.AddWithValue("@BatchSize", batchSize);

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
                AttemptCount = reader.GetInt32(5)
            });
        }

        return results;
    }

    public async Task MarkProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(MarkProcessedSql, connection);
        command.Parameters.AddWithValue("@Id", messageId);
        command.Parameters.AddWithValue("@ProcessedOnUtc", DateTimeOffset.UtcNow);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        logger.LogDebug("Marked outbox message {MessageId} as processed.", messageId);
    }

    public async Task MarkFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(MarkFailedSql, connection);
        command.Parameters.AddWithValue("@Id", messageId);
        command.Parameters.AddWithValue("@Error", error.Length > 2000 ? error[..2000] : error);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        logger.LogWarning("Marked outbox message {MessageId} as failed: {Error}", messageId, error);
    }
}
