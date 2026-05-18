namespace BillingService.Contracts.Events;

public abstract record IntegrationEvent
{
    protected IntegrationEvent(string eventType, string aggregateType, string aggregateId)
    {
        EventType = eventType;
        AggregateType = aggregateType;
        AggregateId = aggregateId;
    }

    public Guid EventId { get; init; } = Guid.NewGuid();

    public string EventType { get; init; }

    public string AggregateType { get; init; }

    public string AggregateId { get; init; }

    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

    public string? CorrelationId { get; init; }

    public int SchemaVersion { get; init; } = 1;
}
