namespace BillingService.Application.Abstractions.Messaging;

/// <summary>
/// Reads unprocessed outbox messages from durable storage for publishing to the event bus.
/// Implementations use SQL polling to retrieve and mark messages in a reliable, retry-safe manner.
/// </summary>
public interface IOutboxPoller
{
    /// <summary>
    /// Retrieves a batch of unprocessed outbox messages ready for publishing.
    /// Messages are not marked processed until <see cref="MarkProcessedAsync"/> is called.
    /// </summary>
    /// <param name="batchSize">Maximum number of messages to retrieve in a single poll.</param>
    Task<IReadOnlyCollection<OutboxMessage>> PollAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an outbox message as successfully processed. Idempotent: safe to call
    /// multiple times for the same message ID.
    /// </summary>
    Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a processing failure for an outbox message, incrementing the attempt count.
    /// Messages exceeding the retry limit are left in a failed state for DLQ inspection.
    /// </summary>
    Task MarkFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default);
}
