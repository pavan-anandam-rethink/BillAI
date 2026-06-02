namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Represents metadata attached to a blob artifact.
/// Blob storage is the authoritative source of truth for all artifacts.
/// Metadata provides correlation, partitioning, and retrieval context without
/// persisting file paths or blob URLs in the relational database.
/// </summary>
public sealed class BlobMetadata
{
    public string CorrelationId { get; init; } = string.Empty;

    public string ContentType { get; init; } = "application/octet-stream";

    /// <summary>Tenant scope identifier used for blob partitioning.</summary>
    public int AccountInfoId { get; init; }

    /// <summary>Business area tag (e.g. "claims", "payments", "edi", "invoices").</summary>
    public string Area { get; init; } = string.Empty;

    /// <summary>Operation or aggregate type tag (e.g. "claim-submission", "era-response").</summary>
    public string OperationType { get; init; } = string.Empty;

    /// <summary>Aggregate identifier this blob relates to (e.g. claim id, payment id).</summary>
    public string AggregateId { get; init; } = string.Empty;

    /// <summary>UTC timestamp used for blob lifecycle and indexing.</summary>
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Caller-supplied additional tags for blob indexing.</summary>
    public IReadOnlyDictionary<string, string> Tags { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Builds the structured blob name that embeds tenant, area, date, and correlation ID.
    /// Pattern: {accountInfoId}/{area}/{yyyy}/{MM}/{dd}/{operationType}/{correlationId}/{fileName}
    /// </summary>
    public string BuildBlobName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Blob file name is required.", nameof(fileName));
        }

        var date = OccurredOnUtc;
        return string.Join(
            "/",
            AccountInfoId.ToString(),
            Normalize(Area),
            date.Year.ToString("D4"),
            date.Month.ToString("D2"),
            date.Day.ToString("D2"),
            Normalize(OperationType),
            CorrelationId,
            fileName);
    }

    private static string Normalize(string value) =>
        string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim().ToLowerInvariant();
}
