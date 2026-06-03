namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Abstracts blob metadata operations.
/// Blob Storage is the authoritative source of all artifacts.
/// The database is NEVER used to store blob paths or physical file references.
/// Implementations retrieve descriptors via blob SDK metadata/tagging rather than
/// SQL path lookups.
/// </summary>
public interface IBlobMetadataStore
{
    /// <summary>
    /// Stores blob metadata tags on the named blob so it can be retrieved by correlation ID
    /// without requiring a SQL path lookup.
    /// </summary>
    Task SetTagsAsync(BlobMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the first blob descriptor that carries the supplied correlation ID tag
    /// within the given container.
    /// Returns null when no matching blob is found.
    /// </summary>
    Task<BlobMetadata?> FindByCorrelationIdAsync(
        string containerName,
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists blob descriptors in the container that match the supplied tag filter.
    /// </summary>
    IAsyncEnumerable<BlobMetadata> ListByTagAsync(
        string containerName,
        string tagKey,
        string tagValue,
        CancellationToken cancellationToken = default);
}
