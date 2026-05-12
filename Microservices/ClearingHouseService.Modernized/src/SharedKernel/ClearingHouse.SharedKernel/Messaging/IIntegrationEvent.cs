namespace ClearingHouse.SharedKernel.Messaging;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    string CorrelationId { get; }
    string EventType { get; }
}

public record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public string CorrelationId { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
}
