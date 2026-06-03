namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Lightweight blob descriptor used for retrieval and metadata-driven orchestration.
/// NEVER contains file paths, blob URLs, or URIs — only the logical blob name within
/// its container and a correlation identifier for tracing.
/// </summary>
public sealed record BlobMetadata
{
    /// <summary>Logical blob name (path-style key within the container, e.g. "claims/2024/01/abc123.edi").</summary>
    public string BlobName { get; init; } = string.Empty;

    /// <summary>Storage container that owns this blob.</summary>
    public string ContainerName { get; init; } = string.Empty;

    /// <summary>Correlation identifier that links this blob to the originating billing operation.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Arbitrary key-value tags stored natively on the blob for indexing and lifecycle policies.</summary>
    public IReadOnlyDictionary<string, string> Tags { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
