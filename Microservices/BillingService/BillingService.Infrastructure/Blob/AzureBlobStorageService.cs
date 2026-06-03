using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BillingService.Application.Abstractions.Blob;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace BillingService.Infrastructure.Blob;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IBlobStorageService"/>.
/// All artifacts (EDI files, clearinghouse payloads, acknowledgements, exports) live
/// exclusively in Blob Storage. No file paths are persisted in the database.
/// </summary>
public sealed class AzureBlobStorageService(
    BlobServiceClient blobServiceClient,
    IOptions<BlobStorageOptions> options,
    ILogger<AzureBlobStorageService> logger)
    : IBlobStorageService
{
    private readonly BlobStorageOptions _options = options.Value;

    public async Task UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        IReadOnlyDictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        ArgumentNullException.ThrowIfNull(content);

        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        if (tags is { Count: > 0 })
        {
            uploadOptions.Tags = new Dictionary<string, string>(tags);
        }

        await blobClient.UploadAsync(content, uploadOptions, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Uploaded blob {BlobName} to container {ContainerName}",
            blobName, containerName);
    }

    public async Task<Stream> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Downloaded blob {BlobName} from container {ContainerName}",
            blobName, containerName);

        return response.Value.Content;
    }

    public async Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var response = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
        return response.Value;
    }

    public async Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.DeleteIfExistsAsync(
            DeleteSnapshotsOption.IncludeSnapshots,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Deleted blob {BlobName} from container {ContainerName}",
            blobName, containerName);
    }

    public async IAsyncEnumerable<BlobMetadata> ListAsync(
        string containerName,
        string? prefix = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);

        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var pages = containerClient
            .GetBlobsAsync(BlobTraits.Tags, BlobStates.None, prefix, cancellationToken)
            .AsPages();

        await foreach (var page in pages.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            foreach (var item in page.Values)
            {
                yield return new BlobMetadata
                {
                    BlobName = item.Name,
                    ContainerName = containerName,
                    Tags = item.Tags ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                };
            }
        }
    }

    public async Task EnsureContainerExistsAsync(
        string containerName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);

        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        try
        {
            await containerClient.CreateIfNotExistsAsync(
                PublicAccessType.None,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ContainerAlreadyExists")
        {
            // Idempotent — container already exists.
        }
    }
}
