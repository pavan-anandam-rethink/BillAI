namespace BillingService.Application.Abstractions.Messaging;

/// <summary>
/// Reads pending outbox messages for the outbox publisher worker.
/// Abstracted here so the worker (BillingService.Workers) remains decoupled from
/// the SQL persistence layer.
/// </summary>
public interface IOutboxReader
{
    /// <summary>
    /// Returns the next batch of unprocessed outbox messages, ordered oldest-first.
    /// The caller is responsible for marking each returned message as processed
    /// via <see cref="MarkProcessedAsync"/> after successful publishing.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> ReadPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as successfully processed.
    /// </summary>
    Task MarkProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a processing failure and increments the attempt counter.
    /// </summary>
    Task RecordFailureAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default);
}
