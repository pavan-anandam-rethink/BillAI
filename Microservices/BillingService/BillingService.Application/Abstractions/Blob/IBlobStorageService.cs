namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Authoritative storage abstraction for all billing artifacts.
/// Blob Storage is the PRIMARY and AUTHORITATIVE storage layer for EDI files,
/// clearinghouse payloads, invoice exports, ERA files, and all other artifacts.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a stream to blob storage and returns the resulting metadata descriptor.
    /// The returned BlobMetadata contains only BlobName and ContainerName — never a file path or URL.
    /// </summary>
    Task<BlobMetadata> UploadAsync(
        Stream content,
        string blobName,
        string containerName,
        string? correlationId = null,
        IDictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads blob content as a stream using the metadata descriptor for lookup.
    /// No file-system path is used; retrieval is blob-native via BlobName and ContainerName.
    /// </summary>
    Task<Stream> DownloadAsync(
        BlobMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when the blob identified by the descriptor exists in storage.
    /// </summary>
    Task<bool> ExistsAsync(
        BlobMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the blob identified by the descriptor.
    /// </summary>
    Task DeleteAsync(
        BlobMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all blob tags attached to the artifact.
    /// Tags are the primary indexing mechanism; no relational path columns are used.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetTagsAsync(
        BlobMetadata metadata,
        CancellationToken cancellationToken = default);
}
