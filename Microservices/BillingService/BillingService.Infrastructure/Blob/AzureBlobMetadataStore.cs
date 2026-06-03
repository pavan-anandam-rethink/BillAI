using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BillingService.Application.Abstractions.Blob;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace BillingService.Infrastructure.Blob;

/// <summary>
/// Azure Blob Storage implementation of IBlobMetadataStore.
/// Implements blob-first architecture: Blob Storage is the PRIMARY and AUTHORITATIVE source.
/// No file paths or blob URLs are ever persisted in the database.
/// Retrieval is performed using BlobName + ContainerName only.
/// </summary>
public sealed class AzureBlobMetadataStore(
    BlobServiceClient blobServiceClient,
    ILogger<AzureBlobMetadataStore> logger)
    : IBlobMetadataStore
{
    public async Task<BlobMetadata> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        IReadOnlyDictionary<string, string>? tags = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var blobClient = container.GetBlobClient(blobName);

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            Conditions = null // Allow overwrite for idempotent re-upload
        };

        if (tags is { Count: > 0 })
        {
            options.Tags = new Dictionary<string, string>(tags);
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            options.Metadata = new Dictionary<string, string>
            {
                ["CorrelationId"] = correlationId
            };
        }

        await blobClient.UploadAsync(content, options, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Uploaded blob {BlobName} to container {ContainerName} with CorrelationId {CorrelationId}",
            blobName, containerName, correlationId);

        return new BlobMetadata(blobName, containerName, correlationId)
        {
            Tags = tags ?? new Dictionary<string, string>()
        };
    }

    public async Task<Stream> DownloadAsync(
        BlobMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        var blobClient = GetBlobClient(metadata);
        var download = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Downloaded blob {BlobName} from container {ContainerName}",
            metadata.BlobName, metadata.ContainerName);

        return download.Value.Content;
    }

    public async Task<bool> ExistsAsync(
        BlobMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        var blobClient = GetBlobClient(metadata);
        var response = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
        return response.Value;
    }

    public async IAsyncEnumerable<BlobMetadata> ListAsync(
        string containerName,
        string? prefix = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);

        await foreach (var item in container
            .GetBlobsAsync(traits: BlobTraits.Metadata | BlobTraits.Tags, prefix: prefix, cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            var correlationId = item.Metadata?.TryGetValue("CorrelationId", out var cid) == true ? cid : null;
            var tags = item.Tags as IReadOnlyDictionary<string, string> ?? new Dictionary<string, string>();

            yield return new BlobMetadata(item.Name, containerName, correlationId) { Tags = tags };
        }
    }

    public async Task<bool> DeleteAsync(
        BlobMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        var blobClient = GetBlobClient(metadata);
        var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (response.Value)
        {
            logger.LogInformation(
                "Deleted blob {BlobName} from container {ContainerName}",
                metadata.BlobName, metadata.ContainerName);
        }

        return response.Value;
    }

    private BlobClient GetBlobClient(BlobMetadata metadata)
    {
        return blobServiceClient
            .GetBlobContainerClient(metadata.ContainerName)
            .GetBlobClient(metadata.BlobName);
    }
}
