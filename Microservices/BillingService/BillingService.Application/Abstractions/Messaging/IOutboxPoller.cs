namespace BillingService.Application.Abstractions.Messaging;

/// <summary>
/// Read-path for the transactional outbox.
/// Implementations query the outbox table and return a batch of unprocessed messages
/// for the <see cref="BillingService.Workers.Outbox.OutboxPublisherWorker"/> to publish.
/// </summary>
public interface IOutboxPoller
{
    /// <summary>
    /// Fetch up to <paramref name="batchSize"/> unprocessed outbox messages.
    /// The caller is responsible for marking each message processed after successful publishing.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> FetchPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark an outbox message as successfully processed.
    /// </summary>
    Task MarkProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a processing failure against a message so it can be retried or sent to DLQ.
    /// </summary>
    Task RecordFailureAsync(
        Guid messageId,
        string errorMessage,
        CancellationToken cancellationToken = default);
}
