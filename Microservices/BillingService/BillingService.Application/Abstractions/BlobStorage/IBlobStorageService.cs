namespace BillingService.Application.Abstractions.BlobStorage;

/// <summary>
/// Blob-first storage abstraction.
/// All clearinghouse files, EDI artifacts, claim request/response documents,
/// patient invoice PDFs, acknowledgements, and export payloads are stored and
/// retrieved exclusively through this service.
/// The database MUST NOT store blob paths or physical file references.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a blob stream to the specified container using a metadata-driven
    /// naming strategy. Returns the assigned blob name which can be used for
    /// downstream correlation but MUST NOT be persisted in the database as a
    /// path reference.
    /// </summary>
    Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        BlobMetadata metadata,
        bool overwrite = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a blob by name. Retrieval is driven by blob-native lookup;
    /// no database record is required.
    /// </summary>
    Task<BlobDownloadResult?> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if a blob with the given name exists in the container.
    /// Used for idempotency checks before re-uploading artifacts.
    /// </summary>
    Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists blob names in the specified container that match an optional prefix.
    /// Uses blob-native prefix/tag queries; no database lookup required.
    /// </summary>
    IAsyncEnumerable<string> ListBlobNamesAsync(
        string containerName,
        string? prefix = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a blob from the specified container.
    /// </summary>
    Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a metadata-driven blob name using the correlation ID, tenant, domain,
    /// document type, and optional suffix. Keeps naming deterministic and correlation-traceable.
    /// </summary>
    string BuildBlobName(BlobMetadata metadata, string? suffix = null);
}
