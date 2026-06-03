namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Configuration options for blob-first storage.
/// Connection information is injected via Key Vault / Kubernetes Secrets;
/// never stored as plain text in source.
/// </summary>
public sealed class BlobStorageOptions
{
    public const string SectionName = "BillingService:BlobStorage";

    /// <summary>Azure Blob Storage connection string (resolved from Key Vault at runtime).</summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>Default container used for EDI and clearinghouse artifacts.</summary>
    public string DefaultContainerName { get; init; } = "rtafiles";

    /// <summary>
    /// Tag key used to index blobs by correlation ID.
    /// Default matches the X-Correlation-Id header convention.
    /// </summary>
    public string CorrelationTagKey { get; init; } = "correlationId";
}
