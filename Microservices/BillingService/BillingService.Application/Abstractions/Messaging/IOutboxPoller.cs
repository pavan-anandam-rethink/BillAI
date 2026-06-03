namespace BillingService.Application.Abstractions.Messaging;

/// <summary>
/// Reads unprocessed outbox messages and marks them as processed after delivery.
/// Implementations must be idempotent: a message already processed must not be re-published.
/// </summary>
public interface IOutboxPoller
{
    /// <summary>
    /// Returns up to <paramref name="batchSize"/> unprocessed outbox messages.
    /// The caller is responsible for publishing each message and then marking it processed
    /// via <see cref="MarkProcessedAsync"/>.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> FetchPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the outbox message identified by <paramref name="messageId"/> as successfully processed.
    /// </summary>
    Task MarkProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a processing failure for the outbox message identified by <paramref name="messageId"/>.
    /// Implementations should increment the attempt count and persist the error detail.
    /// </summary>
    Task MarkFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default);
}
