namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Blob-first storage abstraction.
/// Blob Storage is the PRIMARY and AUTHORITATIVE storage system for all EDI files,
/// clearinghouse artifacts, acknowledgements, and billing documents.
/// The database is NEVER the source of truth for file storage or blob references.
/// </summary>
public interface IBlobMetadataStore
{
    /// <summary>
    /// Uploads content to blob storage idempotently.
    /// Returns metadata describing the stored blob. Metadata NEVER contains file paths or URLs.
    /// </summary>
    Task<BlobMetadata> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        IReadOnlyDictionary<string, string>? tags = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads blob content by logical name. No file-path dependency.
    /// </summary>
    Task<Stream> DownloadAsync(
        BlobMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a blob exists by logical name.
    /// </summary>
    Task<bool> ExistsAsync(
        BlobMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists blobs in a container matching an optional prefix (e.g. a correlation-ID partition prefix).
    /// </summary>
    IAsyncEnumerable<BlobMetadata> ListAsync(
        string containerName,
        string? prefix = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a blob by logical name. Non-destructive: returns false if the blob does not exist.
    /// </summary>
    Task<bool> DeleteAsync(
        BlobMetadata metadata,
        CancellationToken cancellationToken = default);
}
