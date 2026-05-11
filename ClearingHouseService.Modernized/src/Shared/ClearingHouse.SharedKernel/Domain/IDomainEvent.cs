using MediatR;

namespace ClearingHouse.SharedKernel.Domain;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
    string CorrelationId { get; }
}
