namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Primary storage service for all billing artifacts.
/// Blob Storage is the AUTHORITATIVE source for all EDI files, clearinghouse payloads,
/// acknowledgements, exports, and documents. Database is NEVER used for file-path persistence.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Upload content to blob storage. The blob name encodes partition/identity —
    /// no file path is persisted in the database.
    /// </summary>
    Task UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        IReadOnlyDictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download blob content by container and logical blob name.
    /// Retrieval is driven by blob-native naming — no database lookup required.
    /// </summary>
    Task<Stream> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check whether a blob exists without downloading its content.
    /// </summary>
    Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a blob. Idempotent — does not throw if the blob is absent.
    /// </summary>
    Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List blob metadata within a container by tag-based or prefix-based indexing.
    /// Returns <see cref="BlobMetadata"/> descriptors (no paths, no URLs).
    /// </summary>
    IAsyncEnumerable<BlobMetadata> ListAsync(
        string containerName,
        string? prefix = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensure the specified container exists, creating it if necessary.
    /// </summary>
    Task EnsureContainerExistsAsync(
        string containerName,
        CancellationToken cancellationToken = default);
}
