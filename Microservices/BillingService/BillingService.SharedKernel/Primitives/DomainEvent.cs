namespace BillingService.SharedKernel.Primitives;

public abstract record DomainEvent
{
    protected DomainEvent()
    {
        OccurredOnUtc = DateTimeOffset.UtcNow;
    }

    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTimeOffset OccurredOnUtc { get; init; }
}
