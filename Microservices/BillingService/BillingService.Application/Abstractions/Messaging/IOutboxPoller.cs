namespace BillingService.Application.Abstractions.Messaging;

/// <summary>
/// Reads unprocessed outbox messages in order for transactional delivery to the event bus.
/// Implemented in the Persistence layer using the durable outbox table.
/// </summary>
public interface IOutboxPoller
{
    /// <summary>
    /// Returns the next batch of unprocessed outbox messages, oldest first.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> FetchPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an outbox message as successfully processed.
    /// </summary>
    Task MarkProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a processing failure against the outbox message.
    /// Increments AttemptCount and stores the error for observability.
    /// </summary>
    Task MarkFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default);
}
