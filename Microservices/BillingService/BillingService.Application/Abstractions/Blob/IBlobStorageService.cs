namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Blob-first storage abstraction.  All EDI files, clearinghouse payloads, acknowledgements,
/// and artifacts MUST be stored and retrieved through this interface.
/// Database usage is permitted ONLY for lightweight operational metadata — never for file paths.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Upload content to blob storage and return the resulting metadata.
    /// Upload is idempotent: uploading with the same <paramref name="blobName"/> overwrites the previous version.
    /// </summary>
    Task<BlobMetadata> UploadAsync(
        Stream content,
        string blobName,
        string containerName,
        string correlationId,
        IReadOnlyDictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download blob content identified by its logical metadata record.
    /// The returned stream is the caller's responsibility to dispose.
    /// </summary>
    Task<Stream> DownloadAsync(
        BlobMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download blob content identified by blob name and container without a metadata record.
    /// Use when only the blob name and container are known (e.g., from blob tags/indexing).
    /// </summary>
    Task<Stream> DownloadByNameAsync(
        string blobName,
        string containerName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if a blob with the given name exists in the specified container.
    /// </summary>
    Task<bool> ExistsAsync(
        string blobName,
        string containerName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a blob.  No-ops if the blob does not exist.
    /// </summary>
    Task DeleteAsync(
        BlobMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List blob names in a container whose names start with the given prefix.
    /// Used for metadata-driven listing without relying on database records.
    /// </summary>
    IAsyncEnumerable<string> ListBlobNamesAsync(
        string containerName,
        string prefix,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensure the specified container exists, creating it if necessary.
    /// </summary>
    Task EnsureContainerExistsAsync(
        string containerName,
        CancellationToken cancellationToken = default);
}
