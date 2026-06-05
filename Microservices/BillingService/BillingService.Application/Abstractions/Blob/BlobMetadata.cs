namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Lightweight operational metadata for a blob artifact.
/// MUST NOT contain file paths, blob URLs, or URIs.
/// Blob retrieval is performed using BlobName + ContainerName + CorrelationId.
/// </summary>
public sealed record BlobMetadata
{
    public BlobMetadata(string blobName, string containerName, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("BlobName is required.", nameof(blobName));
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("ContainerName is required.", nameof(containerName));
        }

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("CorrelationId is required.", nameof(correlationId));
        }

        BlobName = blobName;
        ContainerName = containerName;
        CorrelationId = correlationId;
    }

    /// <summary>Logical blob name within its container. Not a file path or URL.</summary>
    public string BlobName { get; }

    /// <summary>Target storage container name.</summary>
    public string ContainerName { get; }

    /// <summary>Correlation identifier used for cross-service tracing and blob retrieval.</summary>
    public string CorrelationId { get; }

    public DateTimeOffset CreatedOnUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Blob-native tags for metadata-driven retrieval. Never file paths.</summary>
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
}
