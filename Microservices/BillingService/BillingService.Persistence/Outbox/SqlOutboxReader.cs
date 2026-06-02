using BillingService.Application.Abstractions.Messaging;
using BillingService.Persistence.Legacy;
using Microsoft.Data.SqlClient;

namespace BillingService.Persistence.Outbox;

/// <summary>
/// Reads pending <see cref="OutboxMessage"/> rows from dbo.BillingOutbox and updates
/// their processing status. Used exclusively by <c>OutboxPublisherWorker</c>.
/// </summary>
public sealed class SqlOutboxReader(IBillingSqlConnectionFactory connectionFactory) : IOutboxReader
{
    private const int MaxAttempts = 5;

    private const string SelectPendingSql =
        """
        SELECT TOP (@BatchSize)
               Id, EventType, Payload, CorrelationId,
               OccurredOnUtc, ProcessedOnUtc, ProcessingError, AttemptCount
        FROM   dbo.BillingOutbox WITH (UPDLOCK, READPAST)
        WHERE  ProcessedOnUtc IS NULL
          AND  AttemptCount   < @MaxAttempts
        ORDER BY OccurredOnUtc ASC
        """;

    private const string MarkProcessedSql =
        """
        UPDATE dbo.BillingOutbox
        SET    ProcessedOnUtc = SYSDATETIMEOFFSET()
        WHERE  Id = @Id
        """;

    private const string RecordFailureSql =
        """
        UPDATE dbo.BillingOutbox
        SET    AttemptCount   = AttemptCount + 1,
               ProcessingError = @Error
        WHERE  Id = @Id
        """;

    public async Task<IReadOnlyList<OutboxMessage>> ReadPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<OutboxMessage>();

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(SelectPendingSql, connection);
        command.Parameters.AddWithValue("@BatchSize", batchSize);
        command.Parameters.AddWithValue("@MaxAttempts", MaxAttempts);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            messages.Add(new OutboxMessage
            {
                Id              = reader.GetGuid(reader.GetOrdinal("Id")),
                EventType       = reader.GetString(reader.GetOrdinal("EventType")),
                Payload         = reader.GetString(reader.GetOrdinal("Payload")),
                CorrelationId   = reader.IsDBNull(reader.GetOrdinal("CorrelationId")) ? null : reader.GetString(reader.GetOrdinal("CorrelationId")),
                OccurredOnUtc   = reader.GetDateTimeOffset(reader.GetOrdinal("OccurredOnUtc")),
                ProcessedOnUtc  = reader.IsDBNull(reader.GetOrdinal("ProcessedOnUtc")) ? null : reader.GetDateTimeOffset(reader.GetOrdinal("ProcessedOnUtc")),
                ProcessingError = reader.IsDBNull(reader.GetOrdinal("ProcessingError")) ? null : reader.GetString(reader.GetOrdinal("ProcessingError")),
                AttemptCount    = reader.GetInt32(reader.GetOrdinal("AttemptCount"))
            });
        }

        return messages;
    }

    public async Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(MarkProcessedSql, connection);
        command.Parameters.AddWithValue("@Id", messageId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordFailureAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var truncatedError = error.Length > 1024 ? error[..1024] : error;

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new SqlCommand(RecordFailureSql, connection);
        command.Parameters.AddWithValue("@Id", messageId);
        command.Parameters.AddWithValue("@Error", truncatedError);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
