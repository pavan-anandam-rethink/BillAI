using BillingService.Application.Abstractions.Messaging;
using BillingService.Persistence.Legacy;
using Microsoft.Extensions.Logging;

namespace BillingService.Persistence.Outbox;

/// <summary>
/// SQL-backed implementation of <see cref="IOutboxPoller"/>. Reads pending
/// outbox rows from the BillingOutboxMessages table and marks them as processed
/// after successful delivery.
/// </summary>
internal sealed class SqlOutboxPoller(
    IBillingSqlConnectionFactory connectionFactory,
    ILogger<SqlOutboxPoller> logger)
    : IOutboxPoller
{
    private const string SelectBatchSql = """
        SELECT TOP (@BatchSize)
            Id, EventType, AggregateType, AggregateId, CorrelationId, Payload, CreatedOnUtc
        FROM dbo.BillingOutboxMessages WITH (ROWLOCK, READPAST)
        WHERE ProcessedOnUtc IS NULL
        ORDER BY CreatedOnUtc ASC;
        """;

    private const string MarkProcessedSql = """
        UPDATE dbo.BillingOutboxMessages
        SET    ProcessedOnUtc = SYSUTCDATETIME()
        WHERE  Id = @Id AND ProcessedOnUtc IS NULL;
        """;

    public async Task<IReadOnlyList<PendingOutboxMessage>> ReadPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var rows = new List<PendingOutboxMessage>();

        try
        {
            await using var connection = connectionFactory.CreateOpenConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = SelectBatchSql;
            command.Parameters.AddWithValue("@BatchSize", batchSize);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rows.Add(new PendingOutboxMessage(
                    Id: reader.GetGuid(0),
                    EventType: reader.GetString(1),
                    AggregateType: reader.GetString(2),
                    AggregateId: reader.GetString(3),
                    CorrelationId: reader.IsDBNull(4) ? null : reader.GetString(4),
                    Payload: reader.GetString(5),
                    CreatedOnUtc: new DateTimeOffset(reader.GetDateTime(6), TimeSpan.Zero)));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "BillingService outbox poller: error reading pending messages.");
            throw;
        }

        return rows;
    }

    public async Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = connectionFactory.CreateOpenConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = MarkProcessedSql;
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "BillingService outbox poller: error marking message {Id} as processed.", id);
            throw;
        }
    }
}
