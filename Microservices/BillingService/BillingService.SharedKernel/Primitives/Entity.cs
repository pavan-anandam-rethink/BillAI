namespace BillingService.SharedKernel.Primitives;

public abstract class Entity<TKey>
    where TKey : notnull
{
    private readonly List<DomainEvent> _domainEvents = [];

    protected Entity(TKey id)
    {
        Id = id;
    }

    public TKey Id { get; protected init; }

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public IReadOnlyCollection<DomainEvent> DequeueDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }
}
