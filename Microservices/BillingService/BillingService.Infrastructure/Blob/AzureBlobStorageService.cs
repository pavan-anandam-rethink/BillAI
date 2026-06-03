using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BillingService.Application.Abstractions.Blob;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace BillingService.Infrastructure.Blob;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IBlobStorageService"/>.
/// All content is stored and retrieved directly from blob storage.
/// No file paths or blob URLs are persisted to the database.
/// </summary>
public sealed class AzureBlobStorageService(
    BlobServiceClient blobServiceClient,
    ILogger<AzureBlobStorageService> logger)
    : IBlobStorageService
{
    public async Task<BlobMetadata> UploadAsync(
        Stream content,
        string blobName,
        string containerName,
        string correlationId,
        IReadOnlyDictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var blobClient = containerClient.GetBlobClient(blobName);

        var uploadOptions = new BlobUploadOptions
        {
            Metadata = new Dictionary<string, string>
            {
                ["correlationId"] = correlationId,
                ["uploadedAtUtc"] = DateTimeOffset.UtcNow.ToString("O")
            }
        };

        if (tags is { Count: > 0 })
        {
            uploadOptions.Tags = new Dictionary<string, string>(tags);
        }

        await blobClient.UploadAsync(content, uploadOptions, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Uploaded blob {BlobName} to container {ContainerName} with correlation {CorrelationId}",
            blobName, containerName, correlationId);

        return new BlobMetadata(blobName, containerName, correlationId) { Tags = tags ?? new Dictionary<string, string>() };
    }

    public async Task<Stream> DownloadAsync(BlobMetadata metadata, CancellationToken cancellationToken = default)
    {
        return await DownloadByNameAsync(metadata.BlobName, metadata.ContainerName, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Stream> DownloadByNameAsync(
        string blobName,
        string containerName,
        CancellationToken cancellationToken = default)
    {
        var blobClient = blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Downloaded blob {BlobName} from container {ContainerName}",
            blobName, containerName);

        return response.Value.Content;
    }

    public async Task<bool> ExistsAsync(
        string blobName,
        string containerName,
        CancellationToken cancellationToken = default)
    {
        var blobClient = blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        var response = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
        return response.Value;
    }

    public async Task DeleteAsync(BlobMetadata metadata, CancellationToken cancellationToken = default)
    {
        var blobClient = blobServiceClient
            .GetBlobContainerClient(metadata.ContainerName)
            .GetBlobClient(metadata.BlobName);

        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Deleted blob {BlobName} from container {ContainerName}",
            metadata.BlobName, metadata.ContainerName);
    }

    public async IAsyncEnumerable<string> ListBlobNamesAsync(
        string containerName,
        string prefix,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await foreach (var item in containerClient
            .GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            yield return item.Name;
        }
    }

    public async Task EnsureContainerExistsAsync(
        string containerName,
        CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
