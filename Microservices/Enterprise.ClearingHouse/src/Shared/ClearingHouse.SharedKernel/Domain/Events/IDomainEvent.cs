using ClearingHouse.SharedKernel.Domain.ValueObjects;
using MediatR;

namespace ClearingHouse.SharedKernel.Domain.Events;

/// <summary>
/// Marker interface for domain events, extending MediatR's INotification for in-process handling.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// Gets the correlation ID for distributed tracing.
    /// </summary>
    CorrelationId CorrelationId { get; }
}
