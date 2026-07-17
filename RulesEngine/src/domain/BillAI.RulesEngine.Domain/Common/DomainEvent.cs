namespace BillAI.RulesEngine.Domain.Common;

/// <summary>
/// Base class for domain events, carrying metadata for traceability.
/// </summary>
public abstract class DomainEvent
{
    /// <summary>Gets the unique identifier for this event occurrence.</summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>Gets the UTC timestamp when the event was raised.</summary>
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
