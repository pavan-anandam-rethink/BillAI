namespace ClearingHouse.SharedKernel.Domain.Entities;

/// <summary>
/// Base class for aggregate roots, extending BaseEntity with optimistic concurrency support.
/// </summary>
public abstract class AggregateRoot : BaseEntity
{
    /// <summary>
    /// Gets or sets the version number used for optimistic concurrency control.
    /// </summary>
    public int Version { get; protected set; }

    /// <summary>
    /// Increments the version number and updates the timestamp.
    /// </summary>
    protected void IncrementVersion()
    {
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }
}
