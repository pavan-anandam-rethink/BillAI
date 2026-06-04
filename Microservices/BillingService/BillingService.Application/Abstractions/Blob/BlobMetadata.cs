namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Lightweight blob descriptor used for metadata-driven blob retrieval.
/// MUST NOT contain file paths, absolute URLs, or physical storage location strings.
/// Retrieval is performed by resolving the blob from its container using BlobName.
/// </summary>
public sealed class BlobMetadata
{
    public BlobMetadata(string blobName, string containerName, string? correlationId = null)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("BlobName is required.", nameof(blobName));
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("ContainerName is required.", nameof(containerName));
        }

        BlobName = blobName;
        ContainerName = containerName;
        CorrelationId = correlationId;
    }

    /// <summary>
    /// Logical name of the blob within its container (e.g. "edi/2026/06/04/abc123.x12").
    /// Never a file system path or URI.
    /// </summary>
    public string BlobName { get; }

    /// <summary>
    /// Container that owns this blob (e.g. "billing-edi").
    /// </summary>
    public string ContainerName { get; }

    /// <summary>
    /// Correlation ID linking this blob to the originating business transaction.
    /// Used for distributed tracing and metadata-based retrieval.
    /// </summary>
    public string? CorrelationId { get; }
}
