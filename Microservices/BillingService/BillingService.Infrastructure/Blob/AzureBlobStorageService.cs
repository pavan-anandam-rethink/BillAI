using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BillingService.Application.Abstractions.Blob;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BillingService.Infrastructure.Blob;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IBlobStorageService"/>.
/// All content is stored and retrieved via blob-native operations.
/// No file paths or blob URLs are returned; callers receive BlobMetadata descriptors only.
/// </summary>
public sealed class AzureBlobStorageService(
    BlobServiceClient blobServiceClient,
    IOptions<BlobStorageOptions> options,
    ILogger<AzureBlobStorageService> logger)
    : IBlobStorageService
{
    public async Task<BlobMetadata> UploadAsync(
        Stream content,
        string blobName,
        string containerName,
        string? correlationId = null,
        IDictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);

        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None, cancellationToken: cancellationToken).ConfigureAwait(false);

        var blobClient = containerClient.GetBlobClient(blobName);

        var uploadOptions = new BlobUploadOptions
        {
            Metadata = correlationId is null ? null : new Dictionary<string, string>
            {
                ["correlationId"] = correlationId
            },
            Tags = tags is { Count: > 0 }
                ? new Dictionary<string, string>(tags)
                : null
        };

        await blobClient.UploadAsync(content, uploadOptions, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Uploaded blob {BlobName} to container {ContainerName} with correlationId {CorrelationId}",
            blobName,
            containerName,
            correlationId);

        return new BlobMetadata(blobName, containerName, correlationId);
    }

    public async Task<Stream> DownloadAsync(
        BlobMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var blobClient = GetBlobClient(metadata);
        var response = await blobClient.DownloadStreamingAsync(
            cancellationToken: cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Downloaded blob {BlobName} from container {ContainerName}",
            metadata.BlobName,
            metadata.ContainerName);

        return response.Value.Content;
    }

    public async Task<bool> ExistsAsync(
        BlobMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var blobClient = GetBlobClient(metadata);
        var response = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
        return response.Value;
    }

    public async Task DeleteAsync(
        BlobMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var blobClient = GetBlobClient(metadata);
        await blobClient.DeleteIfExistsAsync(
            DeleteSnapshotsOption.IncludeSnapshots,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Deleted blob {BlobName} from container {ContainerName}",
            metadata.BlobName,
            metadata.ContainerName);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetTagsAsync(
        BlobMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var blobClient = GetBlobClient(metadata);
        var response = await blobClient.GetTagsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.Value.Tags.AsReadOnly();
    }

    // ------------------------------------------------------------------ //

    private BlobClient GetBlobClient(BlobMetadata metadata)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(metadata.ContainerName);
        return containerClient.GetBlobClient(metadata.BlobName);
    }

    // Expose options so DI can validate configuration at startup.
    internal BlobStorageOptions Options => options.Value;
}
