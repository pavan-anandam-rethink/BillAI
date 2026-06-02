using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BillingService.Application.Abstractions.Blob;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace BillingService.Infrastructure.Blob;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IBlobStore"/>.
/// Enforces blob-first architecture: all billing artifacts are stored and retrieved
/// exclusively via this store. File paths and blob URLs are never persisted to SQL.
/// Metadata and structured blob naming drive all retrieval operations.
/// </summary>
public sealed class AzureBlobStore(
    BlobServiceClient blobServiceClient,
    ILogger<AzureBlobStore> logger)
    : IBlobStore
{
    public async Task<string> UploadAsync(
        string containerName,
        string fileName,
        Stream content,
        BlobMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(
            PublicAccessType.None, cancellationToken: cancellationToken).ConfigureAwait(false);

        var blobName = metadata.BuildBlobName(fileName);
        var blobClient = container.GetBlobClient(blobName);

        var httpHeaders = new BlobHttpHeaders { ContentType = metadata.ContentType };

        // Index tags enable server-side filtering without reading blob content.
        var indexTags = BuildIndexTags(metadata);

        var metadataDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["correlationId"] = metadata.CorrelationId,
            ["accountInfoId"] = metadata.AccountInfoId.ToString(),
            ["area"] = metadata.Area,
            ["operationType"] = metadata.OperationType,
            ["aggregateId"] = metadata.AggregateId,
            ["occurredOnUtc"] = metadata.OccurredOnUtc.ToString("O")
        };

        await blobClient.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = httpHeaders,
            Metadata = metadataDict,
            Tags = indexTags
        }, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Blob uploaded: container={Container} name={BlobName} correlationId={CorrelationId}",
            containerName, blobName, metadata.CorrelationId);

        return blobName;
    }

    public async Task<Stream> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var blobClient = blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        var response = await blobClient.DownloadStreamingAsync(
            cancellationToken: cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Blob downloaded: container={Container} name={BlobName}", containerName, blobName);

        return response.Value.Content;
    }

    public async IAsyncEnumerable<string> ListAsync(
        string containerName,
        string prefix,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);

        await foreach (var blob in container.GetBlobsAsync(
            prefix: prefix, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            yield return blob.Name;
        }
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

        var deleted = await blobClient.DeleteIfExistsAsync(
            DeleteSnapshotsOption.IncludeSnapshots,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (deleted.Value)
        {
            logger.LogInformation(
                "Blob deleted: container={Container} name={BlobName}", containerName, blobName);
        }
    }

    public async Task<IReadOnlyDictionary<string, string>> GetTagsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var blobClient = blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        try
        {
            var response = await blobClient.GetTagsAsync(
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return (IReadOnlyDictionary<string, string>)response.Value.Tags;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static Dictionary<string, string> BuildIndexTags(BlobMetadata metadata)
    {
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["correlationId"] = metadata.CorrelationId,
            ["accountInfoId"] = metadata.AccountInfoId.ToString(),
            ["area"] = metadata.Area,
            ["operationType"] = metadata.OperationType
        };

        // Merge caller-supplied tags (tags are limited to 10 per blob by Azure).
        foreach (var (key, value) in metadata.Tags)
        {
            if (tags.Count >= 10)
            {
                break;
            }

            tags[key] = value;
        }

        return tags;
    }
}
