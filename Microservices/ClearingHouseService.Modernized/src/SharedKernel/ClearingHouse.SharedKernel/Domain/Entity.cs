namespace ClearingHouse.SharedKernel.Domain;

public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public string? CreatedBy { get; protected set; }
    public string? UpdatedBy { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);
}
