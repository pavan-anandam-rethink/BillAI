using MediatR;

namespace ClearingHouse.SharedKernel.Domain;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    string CorrelationId { get; }
}

public abstract record DomainEventBase : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string CorrelationId { get; init; } = string.Empty;
}
