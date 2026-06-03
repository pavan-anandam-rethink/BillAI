namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Stores and retrieves lightweight operational metadata about blob artifacts.
/// The database record contains ONLY correlation identifiers and processing state —
/// never file paths, blob URLs, or physical storage references.
/// Blob Storage remains the authoritative source; this metadata enables
/// correlation-ID-driven orchestration without database-centric file tracking.
/// </summary>
public interface IBlobMetadataStore
{
    /// <summary>
    /// Persist minimal blob metadata for operational tracking.
    /// The <paramref name="metadata"/> record must not contain file paths or URLs.
    /// </summary>
    Task SaveAsync(BlobMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve the most recent blob metadata record associated with a correlation ID.
    /// Returns null when no record is found.
    /// </summary>
    Task<BlobMetadata?> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve all blob metadata records for a given container and optional prefix.
    /// Used to discover blobs by partition without relying on database-stored file paths.
    /// </summary>
    Task<IReadOnlyList<BlobMetadata>> ListByContainerAsync(
        string containerName,
        string? blobNamePrefix = null,
        CancellationToken cancellationToken = default);
}
