namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Identifies a blob by its logical name and container.
/// IMPORTANT: This record must NEVER contain file paths, blob URLs, or URIs.
/// Retrieval is always driven by BlobName + ContainerName + CorrelationId.
/// </summary>
public sealed record BlobMetadata
{
    public BlobMetadata(string blobName, string containerName, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("Blob name is required.", nameof(blobName));
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Container name is required.", nameof(containerName));
        }

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("Correlation id is required.", nameof(correlationId));
        }

        BlobName = blobName;
        ContainerName = containerName;
        CorrelationId = correlationId;
    }

    /// <summary>Logical name of the blob within its container (e.g., "claims/2024/01/batch-abc123.edi").</summary>
    public string BlobName { get; }

    /// <summary>Azure Blob Storage container name.</summary>
    public string ContainerName { get; }

    /// <summary>Correlation ID used to trace back to the originating request or workflow.</summary>
    public string CorrelationId { get; }

    /// <summary>Optional additional tags for metadata-driven retrieval (key=value pairs).</summary>
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
}
