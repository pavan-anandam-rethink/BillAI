namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Lightweight operational metadata store for blob tracking.
/// IMPORTANT: Only operational/audit metadata (CorrelationId, processing state, timestamps)
/// is persisted here.  File paths, URLs, or URIs MUST NOT be stored.
/// Blob storage itself is the authoritative source for file content and location.
/// </summary>
public interface IBlobMetadataStore
{
    /// <summary>
    /// Persist operational metadata for a newly uploaded blob.
    /// </summary>
    Task SaveAsync(
        BlobMetadata metadata,
        string category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve the most-recent blob metadata for the given correlation ID and category.
    /// Returns null if no record exists.
    /// </summary>
    Task<BlobMetadata?> FindByCorrelationAsync(
        string correlationId,
        string category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List all blob metadata records for a given category ordered by creation time descending.
    /// </summary>
    IAsyncEnumerable<BlobMetadata> ListByCategoryAsync(
        string category,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}
