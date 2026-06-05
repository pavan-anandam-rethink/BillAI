using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BillingService.Application.Abstractions.Blob;
using Microsoft.Extensions.Logging;

namespace BillingService.Infrastructure.Blob;

public sealed class AzureBlobStorageService(
    BlobServiceClient blobServiceClient,
    ILogger<AzureBlobStorageService> logger)
    : IBlobStorageService
{
    public async Task UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType = "application/octet-stream",
        IDictionary<string, string>? metadata = null,
        IDictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var blobClient = container.GetBlobClient(blobName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            Metadata = metadata,
            Tags = tags
        };

        await blobClient.UploadAsync(content, uploadOptions, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Uploaded blob {BlobName} to container {ContainerName}",
            blobName,
            containerName);
    }

    public async Task<Stream> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var blobClient = blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Downloaded blob {BlobName} from container {ContainerName}",
            blobName,
            containerName);

        return response.Value.Content;
    }

    public async Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var blobClient = blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        var response = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
        return response.Value;
    }

    public async Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var blobClient = blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Deleted blob {BlobName} from container {ContainerName}",
            blobName,
            containerName);
    }

    public async Task<IDictionary<string, string>> GetTagsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var blobClient = blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        var response = await blobClient.GetTagsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return response.Value.Tags;
    }
}
