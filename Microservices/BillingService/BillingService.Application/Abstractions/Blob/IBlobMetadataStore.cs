namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Provides metadata-driven access to Blob Storage artifacts using the Blob-First architecture.
/// Retrieval is based solely on BlobName + ContainerName + CorrelationId; no file paths or
/// physical URI references are exposed or stored by this abstraction.
/// </summary>
public interface IBlobMetadataStore
{
    /// <summary>
    /// Retrieves metadata for a specific blob identified by its container and blob name.
    /// Returns null when the blob does not exist.
    /// </summary>
    Task<BlobMetadata?> GetAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all blobs in a container that carry the given correlation identifier as a tag.
    /// Uses blob-native tag indexing; no database query is involved.
    /// </summary>
    Task<IReadOnlyCollection<BlobMetadata>> FindByCorrelationIdAsync(
        string containerName,
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all blobs in a container that carry a specific tag name/value pair.
    /// Uses blob-native tag indexing for server-side filtering without local enumeration.
    /// </summary>
    Task<IReadOnlyCollection<BlobMetadata>> FindByTagAsync(
        string containerName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies or replaces the tag set on an existing blob.
    /// This is idempotent: calling it multiple times with the same tags produces the same result.
    /// </summary>
    Task SetTagsAsync(
        string containerName,
        string blobName,
        IReadOnlyDictionary<string, string> tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a blob exists in the specified container. Does not retrieve content.
    /// </summary>
    Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);
}
