using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ClearingHouse.SharedKernel.Domain.Interfaces;
using ClearingHouse.SharedKernel.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ClearingHouse.SftpIngestion.Infrastructure.Blob;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IBlobStorageService"/>.
/// Supports streaming upload/download, metadata, and container-per-clearinghouse strategy.
/// </summary>
public sealed class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobStorageService"/> class.
    /// </summary>
    /// <param name="blobServiceClient">The Azure Blob Service client.</param>
    /// <param name="logger">The logger instance.</param>
    public BlobStorageService(BlobServiceClient blobServiceClient, ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FileReference> UploadAsync(
        string containerName,
        string blobPath,
        Stream stream,
        string? contentType = null,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobPath);
        ArgumentNullException.ThrowIfNull(stream);

        _logger.LogDebug("Uploading blob {BlobPath} to container {ContainerName}", blobPath, containerName);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobPath);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType ?? "application/octet-stream"
            },
            Metadata = metadata as Dictionary<string, string> ?? metadata?.ToDictionary(k => k.Key, v => v.Value)
        };

        var response = await blobClient.UploadAsync(stream, uploadOptions, cancellationToken);
        var contentHash = response.Value.ContentHash is not null
            ? Convert.ToHexString(response.Value.ContentHash).ToLowerInvariant()
            : string.Empty;

        var fileSize = stream.CanSeek ? stream.Length : 0;
        var fileName = Path.GetFileName(blobPath);

        _logger.LogInformation(
            "Uploaded blob {BlobPath} to container {ContainerName} ({FileSize} bytes, hash: {ContentHash})",
            blobPath, containerName, fileSize, contentHash);

        return new FileReference(containerName, blobPath, fileName, fileSize, contentHash);
    }

    /// <inheritdoc />
    public async Task<Stream> DownloadStreamAsync(
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobPath);

        _logger.LogDebug("Downloading blob {BlobPath} from container {ContainerName}", blobPath, containerName);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobPath);

        _logger.LogDebug("Deleting blob {BlobPath} from container {ContainerName}", blobPath, containerName);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        _logger.LogInformation("Deleted blob {BlobPath} from container {ContainerName}", blobPath, containerName);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobPath);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        var response = await blobClient.ExistsAsync(cancellationToken);
        return response.Value;
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, string>> GetMetadataAsync(
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobPath);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        return properties.Value.Metadata;
    }

    /// <inheritdoc />
    public async Task MoveAsync(
        string sourceContainer,
        string sourceBlobPath,
        string destinationContainer,
        string destinationBlobPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceContainer);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceBlobPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationContainer);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationBlobPath);

        _logger.LogDebug(
            "Moving blob from {SourceContainer}/{SourceBlobPath} to {DestContainer}/{DestBlobPath}",
            sourceContainer, sourceBlobPath, destinationContainer, destinationBlobPath);

        var sourceContainerClient = _blobServiceClient.GetBlobContainerClient(sourceContainer);
        var sourceBlobClient = sourceContainerClient.GetBlobClient(sourceBlobPath);

        var destContainerClient = _blobServiceClient.GetBlobContainerClient(destinationContainer);
        await destContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var destBlobClient = destContainerClient.GetBlobClient(destinationBlobPath);

        // Copy then delete
        var copyOperation = await destBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri, cancellationToken: cancellationToken);
        await copyOperation.WaitForCompletionAsync(cancellationToken);
        await sourceBlobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Moved blob from {SourceContainer}/{SourceBlobPath} to {DestContainer}/{DestBlobPath}",
            sourceContainer, sourceBlobPath, destinationContainer, destinationBlobPath);
    }
}
