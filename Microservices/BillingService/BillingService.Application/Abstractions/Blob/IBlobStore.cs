namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Abstraction for blob storage operations.
/// Blob storage is the PRIMARY and AUTHORITATIVE storage layer for all billing artifacts:
/// EDI files, clearinghouse requests and responses, ERA files, patient invoices, attachments,
/// exports, and any other document produced or consumed by the billing system.
///
/// The database MUST NOT store file paths, blob URLs, or physical file references.
/// Retrieval is driven by blob-native metadata, tags, and structured naming conventions.
/// </summary>
public interface IBlobStore
{
    /// <summary>
    /// Uploads a stream to blob storage. The blob name is derived from metadata + fileName.
    /// Idempotent: uploading a blob with the same name overwrites the existing blob.
    /// </summary>
    Task<string> UploadAsync(
        string containerName,
        string fileName,
        Stream content,
        BlobMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>Downloads a blob by its fully-qualified name.</summary>
    Task<Stream> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists blob names within a container matching the given prefix.
    /// Use structured name prefixes (e.g. "{accountInfoId}/edi/2024/06/") to scope results.
    /// </summary>
    IAsyncEnumerable<string> ListAsync(
        string containerName,
        string prefix,
        CancellationToken cancellationToken = default);

    /// <summary>Returns true when a blob with the given name exists.</summary>
    Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a blob. Does not throw when the blob does not exist.</summary>
    Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>Returns blob metadata tags for a given blob name.</summary>
    Task<IReadOnlyDictionary<string, string>> GetTagsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);
}
