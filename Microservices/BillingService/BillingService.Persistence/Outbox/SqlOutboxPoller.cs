using BillingService.Application.Abstractions.Messaging;
using BillingService.Persistence.Legacy;
using Microsoft.Extensions.Logging;

namespace BillingService.Persistence.Outbox;

/// <summary>
/// SQL Server-backed implementation of <see cref="IOutboxPoller"/>.
/// Polls billing.OutboxMessages for unprocessed rows and supports idempotent mark-processed/failed operations.
/// Rows with AttemptCount exceeding the retry limit are left in place for operational inspection.
/// </summary>
internal sealed class SqlOutboxPoller(
    IBillingSqlConnectionFactory connectionFactory,
    ILogger<SqlOutboxPoller> logger)
    : IOutboxPoller
{
    private const int MaxAttempts = 5;

    private const string PollSql = """
        SELECT TOP (@BatchSize)
            [Id], [EventType], [Payload], [CorrelationId], [OccurredOnUtc],
            [ProcessedOnUtc], [ProcessingError], [AttemptCount]
        FROM [billing].[OutboxMessages]
        WHERE [ProcessedOnUtc] IS NULL
          AND [AttemptCount] < @MaxAttempts
        ORDER BY [OccurredOnUtc] ASC;
        """;

    private const string MarkProcessedSql = """
        UPDATE [billing].[OutboxMessages]
        SET [ProcessedOnUtc] = @ProcessedOnUtc
        WHERE [Id] = @Id
          AND [ProcessedOnUtc] IS NULL;
        """;

    private const string MarkFailedSql = """
        UPDATE [billing].[OutboxMessages]
        SET [AttemptCount] = [AttemptCount] + 1,
            [ProcessingError] = @Error
        WHERE [Id] = @Id;
        """;

    public async Task<IReadOnlyCollection<OutboxMessage>> PollAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = PollSql;
        command.Parameters.AddWithValue("@BatchSize", batchSize);
        command.Parameters.AddWithValue("@MaxAttempts", MaxAttempts);

        var messages = new List<OutboxMessage>();

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            messages.Add(new OutboxMessage
            {
                Id = reader.GetGuid(0),
                EventType = reader.GetString(1),
                Payload = reader.GetString(2),
                CorrelationId = reader.IsDBNull(3) ? null : reader.GetString(3),
                OccurredOnUtc = reader.GetFieldValue<DateTimeOffset>(4),
                ProcessedOnUtc = reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5),
                ProcessingError = reader.IsDBNull(6) ? null : reader.GetString(6),
                AttemptCount = reader.GetInt32(7)
            });
        }

        if (messages.Count > 0)
        {
            logger.LogDebug("Polled {Count} outbox messages for publishing.", messages.Count);
        }

        return messages.AsReadOnly();
    }

    public async Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = MarkProcessedSql;
        command.Parameters.AddWithValue("@Id", messageId);
        command.Parameters.AddWithValue("@ProcessedOnUtc", DateTimeOffset.UtcNow);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        if (rows == 0)
        {
            // Already processed by another worker instance – idempotent, so this is expected.
            logger.LogDebug("Outbox message {MessageId} was already marked processed.", messageId);
        }
    }

    public async Task MarkFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = MarkFailedSql;
        command.Parameters.AddWithValue("@Id", messageId);
        command.Parameters.AddWithValue("@Error",
            error.Length > 1000 ? error[..1000] : error);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        logger.LogWarning(
            "Outbox message {MessageId} failed processing. Error recorded for retry.",
            messageId);
    }
}
