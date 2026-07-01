namespace ClaimLifecycleMonitoring.Domain.Common;

/// <summary>
/// Base type for all persistent domain entities. Provides an identity property
/// and audit metadata that is populated by the persistence layer.
/// </summary>
public abstract class EntityBase
{
    /// <summary>Primary key. Populated by the persistence layer.</summary>
    public long Id { get; set; }

    /// <summary>Timestamp (UTC) the entity was created.</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Timestamp (UTC) the entity was last modified.</summary>
    public DateTime? ModifiedUtc { get; set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[]? RowVersion { get; set; }
}
