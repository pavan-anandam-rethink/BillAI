using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BillingService.Application.Abstractions.BlobStorage;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;

namespace BillingService.Infrastructure.BlobStorage;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IBlobStorageService"/>.
/// Follows a blob-first, metadata-driven architecture: all artifact retrieval
/// is driven by blob-native capabilities (tags, metadata, naming convention).
/// No file paths are persisted in the relational database.
/// </summary>
internal sealed class AzureBlobStorageService(
    BlobServiceClient blobServiceClient,
    ILogger<AzureBlobStorageService> logger)
    : IBlobStorageService
{
    private const char PathSeparator = '/';

    public async Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        BlobMetadata metadata,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken).ConfigureAwait(false);

        var blobClient = container.GetBlobClient(blobName);
        var httpHeaders = new BlobHttpHeaders { ContentType = metadata.ContentType };

        var blobMetadata = BuildAzureMetadata(metadata);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = httpHeaders,
            Metadata = blobMetadata,
            Conditions = overwrite ? null : new BlobRequestConditions { IfNoneMatch = ETag.All }
        };

        await blobClient.UploadAsync(content, uploadOptions, cancellationToken).ConfigureAwait(false);

        if (metadata.Tags.Count > 0)
        {
            await blobClient.SetTagsAsync(metadata.Tags.ToDictionary(kv => kv.Key, kv => kv.Value), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        logger.LogInformation(
            "Uploaded blob {BlobName} to container {Container} [CorrelationId={CorrelationId} Domain={Domain} DocumentType={DocumentType}]",
            blobName, containerName, metadata.CorrelationId, metadata.Domain, metadata.DocumentType);

        return blobName;
    }

    public async Task<BlobDownloadResult?> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = container.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            logger.LogWarning("Blob {BlobName} not found in container {Container}", blobName, containerName);
            return null;
        }

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var props = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var azureMetadata = props.Value.Metadata;

        var metadata = new BlobMetadata
        {
            CorrelationId = azureMetadata.GetValueOrDefault("correlationid", string.Empty),
            TenantId = azureMetadata.GetValueOrDefault("tenantid", string.Empty),
            Domain = azureMetadata.GetValueOrDefault("domain", string.Empty),
            DocumentType = azureMetadata.GetValueOrDefault("documenttype", string.Empty),
            CreatedOnUtc = azureMetadata.GetValueOrDefault("createdonutc", string.Empty),
            ContentType = props.Value.ContentType
        };

        return new BlobDownloadResult(response.Value.Content, metadata, blobName, containerName);
    }

    public async Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = container.GetBlobClient(blobName);
        var response = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
        return response.Value;
    }

    public async IAsyncEnumerable<string> ListBlobNamesAsync(
        string containerName,
        string? prefix = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);

        await foreach (var item in container.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            yield return item.Name;
        }
    }

    public async Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = container.GetBlobClient(blobName);
        var deleted = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        if (deleted.Value)
        {
            logger.LogInformation("Deleted blob {BlobName} from container {Container}", blobName, containerName);
        }
    }

    public string BuildBlobName(BlobMetadata metadata, string? suffix = null)
    {
        // Pattern: {domain}/{tenantId}/{yyyy/MM/dd}/{correlationId}_{documentType}[_suffix]
        var date = DateTimeOffset.UtcNow;
        var sb = new StringBuilder();

        Append(sb, metadata.Domain);
        sb.Append(PathSeparator);
        Append(sb, metadata.TenantId);
        sb.Append(PathSeparator);
        sb.Append(date.Year.ToString("D4"));
        sb.Append(PathSeparator);
        sb.Append(date.Month.ToString("D2"));
        sb.Append(PathSeparator);
        sb.Append(date.Day.ToString("D2"));
        sb.Append(PathSeparator);
        Append(sb, metadata.CorrelationId);
        sb.Append('_');
        Append(sb, metadata.DocumentType);

        if (!string.IsNullOrWhiteSpace(suffix))
        {
            sb.Append('_');
            Append(sb, suffix);
        }

        return sb.ToString();

        static void Append(StringBuilder b, string value)
        {
            b.Append(string.IsNullOrWhiteSpace(value) ? "unknown" : value.ToLowerInvariant().Trim());
        }
    }

    private static Dictionary<string, string> BuildAzureMetadata(BlobMetadata metadata)
    {
        // Azure metadata keys must be valid C# identifiers (no hyphens)
        return new Dictionary<string, string>
        {
            ["correlationid"] = metadata.CorrelationId,
            ["tenantid"] = metadata.TenantId,
            ["domain"] = metadata.Domain,
            ["documenttype"] = metadata.DocumentType,
            ["createdonutc"] = metadata.CreatedOnUtc,
            ["contenttype"] = metadata.ContentType
        };
    }
}
