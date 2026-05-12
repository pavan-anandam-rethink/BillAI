namespace ClearingHouse.SharedKernel.Domain;

public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    public long Version { get; protected set; }

    protected void IncrementVersion()
    {
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }
}
