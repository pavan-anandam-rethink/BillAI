namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Configuration options for Blob-First architecture. Contains no connection strings or secrets;
/// those are resolved from Azure Key Vault at runtime.
/// </summary>
public sealed class BlobStorageOptions
{
    public const string SectionName = "BillingService:BlobStorage";

    /// <summary>
    /// Default container used for general billing artifacts such as claim attachments and reports.
    /// </summary>
    public string DefaultContainerName { get; init; } = "rtafiles";

    /// <summary>
    /// Container used for outbound EDI/clearinghouse request files (e.g. 837P/I).
    /// </summary>
    public string EdiRequestContainerName { get; init; } = "edi-requests";

    /// <summary>
    /// Container used for inbound clearinghouse response files (e.g. 835, 999, 277).
    /// </summary>
    public string EdiResponseContainerName { get; init; } = "edi-responses";

    /// <summary>
    /// Container used for ERA (Electronic Remittance Advice) manual uploads.
    /// </summary>
    public string EraUploadContainerName { get; init; } = "eramanualupload";

    /// <summary>
    /// Container used for Availity clearinghouse submissions and responses.
    /// </summary>
    public string AvailityContainerName { get; init; } = "availity";

    /// <summary>
    /// Tag name used to index blobs by correlation ID for metadata-driven retrieval.
    /// </summary>
    public string CorrelationIdTagName { get; init; } = "correlationId";

    /// <summary>
    /// Maximum number of blob metadata entries returned by a single tag-based query.
    /// </summary>
    public int MaxTagQueryResults { get; init; } = 500;
}
