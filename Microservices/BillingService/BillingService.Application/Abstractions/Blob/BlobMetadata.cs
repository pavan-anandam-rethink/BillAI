namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Represents the metadata for a blob artifact in Blob-First storage architecture.
/// IMPORTANT: This record must NEVER contain file paths, blob URLs, or physical URIs.
/// Blob retrieval is driven by BlobName + ContainerName + CorrelationId only.
/// Blob Storage is the authoritative source of truth; the database is never used for file references.
/// </summary>
public sealed record BlobMetadata
{
    /// <summary>
    /// The logical name of the blob within its container (e.g. "edi/837/2024/01/claim-abc.edi").
    /// Never a file-system path or a full URI.
    /// </summary>
    public required string BlobName { get; init; }

    /// <summary>
    /// The Blob Storage container that holds this blob (e.g. "rtafiles", "edi-requests").
    /// </summary>
    public required string ContainerName { get; init; }

    /// <summary>
    /// Correlation identifier used to associate this blob with a request, transaction, or workflow.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Blob tags attached to this artifact. Used for server-side blob filtering and indexing.
    /// Tag values are limited to 256 characters per Azure Blob Storage constraints.
    /// </summary>
    public IReadOnlyDictionary<string, string> Tags { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The UTC timestamp when this blob metadata was captured. Informational only; the blob
    /// itself is the authoritative record in Azure Blob Storage.
    /// </summary>
    public DateTimeOffset? CapturedAtUtc { get; init; }
}
