namespace BillingService.Application.Abstractions.Messaging;

/// <summary>
/// Reads unprocessed outbox messages for relay to the event bus.
/// The durable outbox pattern guarantees at-least-once delivery:
/// messages are persisted transactionally with their owning business entity
/// then relayed to Service Bus by the OutboxPublisherWorker.
/// </summary>
public interface IOutboxPoller
{
    /// <summary>
    /// Returns up to <paramref name="batchSize"/> unprocessed outbox messages
    /// ordered by OccurredOnUtc ascending.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> PollAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the given messages as successfully processed.
    /// </summary>
    Task MarkProcessedAsync(
        IEnumerable<Guid> messageIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a processing failure for a single message.
    /// The message will be retried on the next poll cycle unless the max attempt count is exceeded.
    /// </summary>
    Task RecordFailureAsync(
        Guid messageId,
        string errorMessage,
        CancellationToken cancellationToken = default);
}
