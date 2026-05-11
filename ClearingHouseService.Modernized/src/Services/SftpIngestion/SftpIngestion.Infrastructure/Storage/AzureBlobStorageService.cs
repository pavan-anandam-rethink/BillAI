using System.Runtime.CompilerServices;
using Azure.Storage.Blobs;
using ClearingHouse.SharedKernel.Interfaces;
using Microsoft.Extensions.Logging;

namespace SftpIngestion.Infrastructure.Storage;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(BlobServiceClient blobServiceClient, ILogger<AzureBlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<string> UploadAsync(string containerName, string blobName, Stream content, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(content, overwrite: true, cancellationToken: cancellationToken);
        if (metadata is not null)
            await blob.SetMetadataAsync(metadata, cancellationToken: cancellationToken);
        _logger.LogInformation("Uploaded blob {BlobName} to container {ContainerName}", blobName, containerName);
        return blob.Uri.ToString();
    }

    public async Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        var blob = _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        var blob = _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        var blob = _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
        return await blob.ExistsAsync(cancellationToken);
    }

    public async Task<IDictionary<string, string>> GetMetadataAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        var blob = _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
        var props = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);
        return props.Value.Metadata;
    }

    public async Task SetMetadataAsync(string containerName, string blobName, IDictionary<string, string> metadata, CancellationToken cancellationToken = default)
    {
        var blob = _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
        await blob.SetMetadataAsync(metadata, cancellationToken: cancellationToken);
    }

    public async Task MoveAsync(string sourceContainer, string sourceBlobName, string destContainer, string destBlobName, CancellationToken cancellationToken = default)
    {
        var sourceBlob = _blobServiceClient.GetBlobContainerClient(sourceContainer).GetBlobClient(sourceBlobName);
        var destContainerClient = _blobServiceClient.GetBlobContainerClient(destContainer);
        await destContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var destBlob = destContainerClient.GetBlobClient(destBlobName);

        var copyOp = await destBlob.StartCopyFromUriAsync(sourceBlob.Uri, cancellationToken: cancellationToken);
        await copyOp.WaitForCompletionAsync(cancellationToken);
        await sourceBlob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async IAsyncEnumerable<string> ListBlobsAsync(string containerName, string? prefix = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await foreach (var blob in container.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
        {
            yield return blob.Name;
        }
    }
}
