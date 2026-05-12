using ClearingHouse.SharedKernel.Domain.ValueObjects;

namespace ClearingHouse.SharedKernel.Infrastructure.ServiceBus;

/// <summary>
/// Base class for integration events published to the service bus for cross-service communication.
/// </summary>
public abstract record IntegrationEvent
{
    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the correlation ID for distributed tracing.
    /// </summary>
    public CorrelationId CorrelationId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the fully qualified type name of this event.
    /// </summary>
    public string EventType => GetType().FullName!;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationEvent"/> record.
    /// </summary>
    /// <param name="correlationId">The correlation ID for tracing.</param>
    protected IntegrationEvent(CorrelationId correlationId)
    {
        CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
    }
}
