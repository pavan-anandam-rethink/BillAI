namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Persists lightweight operational metadata for blobs.
/// ONLY BlobName, ContainerName, and CorrelationId are stored in the database.
/// File paths, blob URLs, and physical storage references are NEVER persisted here.
/// Blob Storage itself remains the authoritative source of artifact content.
/// </summary>
public interface IBlobMetadataStore
{
    /// <summary>
    /// Persists blob metadata for correlation-based retrieval.
    /// No file path or URL is stored.
    /// </summary>
    Task SaveAsync(BlobMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves blob metadata by correlation ID.
    /// Returns all blobs associated with the given correlation identifier.
    /// </summary>
    Task<IReadOnlyList<BlobMetadata>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single blob metadata entry by its blob name within a container.
    /// Returns null when no matching record exists.
    /// </summary>
    Task<BlobMetadata?> GetByBlobNameAsync(
        string blobName,
        string containerName,
        CancellationToken cancellationToken = default);
}
