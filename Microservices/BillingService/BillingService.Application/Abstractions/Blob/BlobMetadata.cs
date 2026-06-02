namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Lightweight blob artifact metadata persisted for audit and correlation.
///
/// IMPORTANT: This record must NEVER carry a file path, a blob URL, or any other
/// storage-location reference. The <see cref="BlobName"/> is used solely to
/// reconstruct the blob address at retrieval time by combining it with the
/// well-known container name for the given <see cref="Category"/>.
/// Blob Storage itself is the authoritative store of content.
/// </summary>
public sealed record BlobMetadata
{
    /// <summary>Surrogate primary key for the operational audit row.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The logical blob name within the container.  Constructed from correlation ID,
    /// partitioning prefix, and a deterministic suffix so retrieval is possible
    /// without a database lookup.
    /// Example: "edi/2024/06/02/{correlationId}/837-claim.x12"
    /// </summary>
    public required string BlobName { get; init; }

    /// <summary>
    /// Blob container name.  Must be one of the well-known containers defined in
    /// the blob-first naming strategy (e.g. "rtafiles", "availity", "eramanualupload").
    /// NOT stored as a physical path or URL.
    /// </summary>
    public required string ContainerName { get; init; }

    /// <summary>
    /// Functional category for the artifact.
    /// Examples: "edi-837", "edi-835", "edi-999", "edi-277", "clearinghouse-ack",
    ///           "era-upload", "era-error", "patient-invoice-pdf".
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Tenant that produced or owns this artifact.
    /// </summary>
    public int AccountInfoId { get; init; }

    /// <summary>
    /// Correlation ID linking this artifact to the request or job that created it.
    /// Used for distributed trace join and cross-service retrieval.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// Optional business aggregate identifier (e.g. claim ID, payment ID).
    /// </summary>
    public string? AggregateId { get; init; }

    /// <summary>
    /// Optional aggregate type label (e.g. "Claim", "Payment", "PatientInvoice").
    /// </summary>
    public string? AggregateType { get; init; }

    /// <summary>Content type of the blob (e.g. "application/x-edi-x12", "application/pdf").</summary>
    public string ContentType { get; init; } = "application/octet-stream";

    /// <summary>Approximate blob size in bytes at upload time.  0 when unknown.</summary>
    public long SizeBytes { get; init; }

    /// <summary>UTC instant when this artifact was produced.</summary>
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Current processing state of the artifact.</summary>
    public BlobProcessingStatus Status { get; init; } = BlobProcessingStatus.Uploaded;

    /// <summary>Reason for failure, populated only when Status is Failed.</summary>
    public string? FailureReason { get; init; }
}

public enum BlobProcessingStatus
{
    Uploaded = 0,
    Processing = 1,
    Processed = 2,
    Failed = 3
}
