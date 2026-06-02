namespace BillingService.Application.Abstractions.BlobStorage;

/// <summary>
/// Immutable metadata bundle attached to every blob artifact.
/// Blob storage is the authoritative source of truth; no file paths are persisted in the database.
/// </summary>
public sealed record BlobMetadata
{
    /// <summary>Correlation ID that links the blob to a business transaction.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Logical tenant identifier (AccountInfoId).</summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>Domain area tag (e.g. "claims", "payments", "edi", "invoices").</summary>
    public string Domain { get; init; } = string.Empty;

    /// <summary>Business document type (e.g. "837", "835", "999", "invoice-pdf").</summary>
    public string DocumentType { get; init; } = string.Empty;

    /// <summary>ISO-8601 UTC timestamp when the blob was created.</summary>
    public string CreatedOnUtc { get; init; } = DateTimeOffset.UtcNow.ToString("O");

    /// <summary>MIME content type of the stored artifact.</summary>
    public string ContentType { get; init; } = "application/octet-stream";

    /// <summary>Additional arbitrary tags for blob indexing.</summary>
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
}
