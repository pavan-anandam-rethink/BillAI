using ClearingHouse.SharedKernel.Domain.ValueObjects;

namespace ClearingHouse.SharedKernel.Domain.Interfaces;

/// <summary>
/// Abstraction for blob storage operations.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a stream to blob storage.
    /// </summary>
    /// <param name="containerName">The target container name.</param>
    /// <param name="blobPath">The blob path within the container.</param>
    /// <param name="stream">The content stream to upload.</param>
    /// <param name="contentType">The MIME content type.</param>
    /// <param name="metadata">Optional metadata key-value pairs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A file reference to the uploaded blob.</returns>
    Task<FileReference> UploadAsync(
        string containerName,
        string blobPath,
        Stream stream,
        string? contentType = null,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a blob as a stream.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    /// <param name="blobPath">The blob path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream of the blob content.</returns>
    Task<Stream> DownloadStreamAsync(
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a blob from storage.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    /// <param name="blobPath">The blob path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a blob exists.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    /// <param name="blobPath">The blob path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the blob exists; otherwise false.</returns>
    Task<bool> ExistsAsync(
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the metadata for a blob.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    /// <param name="blobPath">The blob path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The metadata dictionary.</returns>
    Task<IDictionary<string, string>> GetMetadataAsync(
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a blob from one location to another.
    /// </summary>
    /// <param name="sourceContainer">The source container name.</param>
    /// <param name="sourceBlobPath">The source blob path.</param>
    /// <param name="destinationContainer">The destination container name.</param>
    /// <param name="destinationBlobPath">The destination blob path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MoveAsync(
        string sourceContainer,
        string sourceBlobPath,
        string destinationContainer,
        string destinationBlobPath,
        CancellationToken cancellationToken = default);
}
