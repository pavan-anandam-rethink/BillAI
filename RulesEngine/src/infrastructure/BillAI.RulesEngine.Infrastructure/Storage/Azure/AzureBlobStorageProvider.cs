using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BillAI.RulesEngine.Application.Interfaces.Services;

namespace BillAI.RulesEngine.Infrastructure.Storage.Azure;

/// <summary>
/// Azure Blob Storage-based storage provider.
/// Each tenant gets its own blob container: rules-engine-{tenantId}
/// </summary>
public sealed class AzureBlobStorageProvider(BlobServiceClient blobServiceClient) : IStorageProvider
{
    private BlobContainerClient GetContainerClient(string tenantId)
    {
        var containerName = $"rules-engine-{tenantId.ToLowerInvariant()}";
        return blobServiceClient.GetBlobContainerClient(containerName);
    }

    private async Task<BlobContainerClient> EnsureContainerAsync(string tenantId, CancellationToken ct)
    {
        var container = GetContainerClient(tenantId);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        return container;
    }

    /// <inheritdoc/>
    public async Task<string?> ReadAsync(string tenantId, string key, CancellationToken ct = default)
    {
        var container = GetContainerClient(tenantId);
        var blob = container.GetBlobClient(key);

        if (!await blob.ExistsAsync(ct)) return null;

        var response = await blob.DownloadContentAsync(ct);
        return response.Value.Content.ToString();
    }

    /// <inheritdoc/>
    public async Task WriteAsync(string tenantId, string key, string content, CancellationToken ct = default)
    {
        var container = await EnsureContainerAsync(tenantId, ct);
        var blob = container.GetBlobClient(key);
        await blob.UploadAsync(BinaryData.FromString(content), overwrite: true, ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string tenantId, string key, CancellationToken ct = default)
    {
        var container = GetContainerClient(tenantId);
        var blob = container.GetBlobClient(key);
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string tenantId, string key, CancellationToken ct = default)
    {
        var container = GetContainerClient(tenantId);
        var blob = container.GetBlobClient(key);
        return await blob.ExistsAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> ListKeysAsync(string tenantId, string? prefix = null, CancellationToken ct = default)
    {
        var container = GetContainerClient(tenantId);

        var keys = new List<string>();
        await foreach (var item in container.GetBlobsAsync(prefix: prefix, cancellationToken: ct))
            keys.Add(item.Name);

        return keys;
    }
}
