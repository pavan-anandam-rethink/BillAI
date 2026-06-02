namespace BillingService.Application.Abstractions.Messaging;

/// <summary>
/// Represents a single outbox row ready for publishing to the event bus.
/// </summary>
public sealed record PendingOutboxMessage(
    Guid Id,
    string EventType,
    string AggregateType,
    string AggregateId,
    string? CorrelationId,
    string Payload,
    DateTimeOffset CreatedOnUtc);

/// <summary>
/// Reads pending outbox messages and marks them as processed after successful
/// delivery.  Implementations use the SQL persistence store to fetch rows
/// that have not yet been published to the event bus.
/// </summary>
public interface IOutboxPoller
{
    /// <summary>
    /// Returns up to <paramref name="batchSize"/> unprocessed outbox messages
    /// ordered by creation time ascending (oldest first).
    /// </summary>
    Task<IReadOnlyList<PendingOutboxMessage>> ReadPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the outbox message identified by <paramref name="id"/> as processed.
    /// Must be called only after the message has been durably published to the bus.
    /// </summary>
    Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default);
}
