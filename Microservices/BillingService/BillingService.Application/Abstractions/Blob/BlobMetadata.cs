namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Blob-first storage descriptor.
/// IMPORTANT: This record MUST NOT contain file paths, physical URIs, or URL strings.
/// Retrieval relies solely on BlobName + ContainerName navigated through the blob SDK.
/// CorrelationId is carried as blob metadata/tag for cross-service tracing only.
/// </summary>
public sealed record BlobMetadata
{
    /// <summary>Logical blob name within the container (e.g. "837/2024/06/clm-xyz.edi").</summary>
    public required string BlobName { get; init; }

    /// <summary>Azure Blob Storage container that owns this blob.</summary>
    public required string ContainerName { get; init; }

    /// <summary>Correlation identifier propagated as blob metadata for distributed tracing.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Additional metadata key/value pairs stored as blob metadata tags.</summary>
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
}
