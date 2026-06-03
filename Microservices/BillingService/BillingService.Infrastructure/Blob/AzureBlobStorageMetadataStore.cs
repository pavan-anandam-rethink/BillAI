using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BillingService.Application.Abstractions.Blob;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BillingService.Infrastructure.Blob;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IBlobMetadataStore"/>.
/// Retrieval is driven exclusively by BlobName, ContainerName, and blob tags (CorrelationId).
/// No file paths, blob URIs, or SQL references are used or exposed.
/// </summary>
internal sealed class AzureBlobStorageMetadataStore(
    BlobServiceClient blobServiceClient,
    IOptions<BlobStorageOptions> options,
    ILogger<AzureBlobStorageMetadataStore> logger)
    : IBlobMetadataStore
{
    private readonly BlobStorageOptions _options = options.Value;

    public async Task<BlobMetadata?> GetAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var blobClient = GetBlobClient(containerName, blobName);

        try
        {
            var exists = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (!exists.Value)
            {
                return null;
            }

            var tagsResponse = await blobClient.GetTagsAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var tags = tagsResponse.Value.Tags
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

            tags.TryGetValue(_options.CorrelationIdTagName, out var correlationId);

            return new BlobMetadata
            {
                BlobName = blobName,
                ContainerName = containerName,
                CorrelationId = correlationId,
                Tags = tags,
                CapturedAtUtc = DateTimeOffset.UtcNow
            };
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex,
                "Failed to retrieve blob metadata for {ContainerName}/{BlobName}",
                containerName, blobName);
            throw;
        }
    }

    public async Task<IReadOnlyCollection<BlobMetadata>> FindByCorrelationIdAsync(
        string containerName,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        return await FindByTagAsync(
            containerName,
            _options.CorrelationIdTagName,
            correlationId,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<BlobMetadata>> FindByTagAsync(
        string containerName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var results = new List<BlobMetadata>();

        // Azure Blob Storage tag-based filter uses OData-style query syntax.
        var tagFilter = $"\"{tagName}\" = '{tagValue}'";

        try
        {
            await foreach (var taggedBlob in blobServiceClient
                .FindBlobsByTagsAsync(tagFilter, cancellationToken)
                .ConfigureAwait(false))
            {
                if (!string.Equals(taggedBlob.BlobContainerName, containerName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (results.Count >= _options.MaxTagQueryResults)
                {
                    logger.LogWarning(
                        "Tag query for {ContainerName} tag={TagName}:{TagValue} reached limit of {Limit} results.",
                        containerName, tagName, tagValue, _options.MaxTagQueryResults);
                    break;
                }

                results.Add(new BlobMetadata
                {
                    BlobName = taggedBlob.BlobName,
                    ContainerName = containerName,
                    CorrelationId = taggedBlob.Tags.TryGetValue(_options.CorrelationIdTagName, out var cid)
                        ? cid
                        : null,
                    Tags = taggedBlob.Tags
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase),
                    CapturedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex,
                "Failed to query blobs by tag {TagName}={TagValue} in {ContainerName}",
                tagName, tagValue, containerName);
            throw;
        }

        return results.AsReadOnly();
    }

    public async Task SetTagsAsync(
        string containerName,
        string blobName,
        IReadOnlyDictionary<string, string> tags,
        CancellationToken cancellationToken = default)
    {
        var blobClient = GetBlobClient(containerName, blobName);

        try
        {
            await blobClient.SetTagsAsync(
                new Dictionary<string, string>(tags, StringComparer.OrdinalIgnoreCase),
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex,
                "Failed to set tags on blob {ContainerName}/{BlobName}",
                containerName, blobName);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var blobClient = GetBlobClient(containerName, blobName);

        try
        {
            var response = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
            return response.Value;
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex,
                "Failed to check existence for blob {ContainerName}/{BlobName}",
                containerName, blobName);
            throw;
        }
    }

    private BlobClient GetBlobClient(string containerName, string blobName)
    {
        return blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);
    }
}
