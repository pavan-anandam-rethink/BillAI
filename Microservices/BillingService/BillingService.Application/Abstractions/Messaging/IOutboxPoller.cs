using BillingService.Application.Abstractions.Messaging;

namespace BillingService.Application.Abstractions.Messaging;

/// <summary>
/// Reads pending outbox messages for publication by the outbox worker.
/// Abstracts the persistence technology so the worker only depends on the Application layer.
/// </summary>
public interface IOutboxPoller
{
    /// <summary>
    /// Fetch a batch of unprocessed outbox messages ordered by OccurredOnUtc ascending.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> FetchPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark an outbox message as successfully published.
    /// </summary>
    Task MarkProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a delivery failure for an outbox message, incrementing the attempt count
    /// and storing the error reason. The message remains eligible for retry until
    /// a maximum attempt threshold is enforced at the call site.
    /// </summary>
    Task RecordFailureAsync(
        Guid messageId,
        string errorReason,
        CancellationToken cancellationToken = default);
}
