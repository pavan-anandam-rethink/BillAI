namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Identifies a blob by its logical name within a container.
/// MUST NOT contain file paths, blob URLs, or URIs.
/// Retrieval is performed via BlobName + ContainerName + CorrelationId only.
/// </summary>
public sealed class BlobMetadata
{
    public BlobMetadata(string blobName, string containerName, string? correlationId = null)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("Blob name is required.", nameof(blobName));
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Container name is required.", nameof(containerName));
        }

        BlobName = blobName;
        ContainerName = containerName;
        CorrelationId = correlationId;
    }

    /// <summary>Logical name of the blob within the container (e.g. "edi/2024/01/claim-abc123.x12").</summary>
    public string BlobName { get; }

    /// <summary>Name of the Azure Blob Storage container.</summary>
    public string ContainerName { get; }

    /// <summary>Correlation ID used to trace this blob across systems.</summary>
    public string? CorrelationId { get; }

    /// <summary>Optional key-value metadata tags stored on the blob object.</summary>
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
}
