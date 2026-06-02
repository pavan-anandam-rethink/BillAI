namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Configuration for the billing-specific Azure Blob Storage account.
/// Connection details are resolved from Azure Key Vault at runtime.
/// </summary>
public sealed class BlobStorageOptions
{
    public const string SectionName = "BillingService:BlobStorage";

    /// <summary>
    /// Azure Blob Storage connection string.  Must be loaded from Key Vault.
    /// Never stored in appsettings or environment variables in plain text.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>
    /// Default container for EDI request/response artifacts.
    /// </summary>
    public string EdiContainerName { get; init; } = "rtafiles";

    /// <summary>
    /// Container for ERA uploads and parsed payment files.
    /// </summary>
    public string EraContainerName { get; init; } = "eramanualupload";

    /// <summary>
    /// Container for Availity response files.
    /// </summary>
    public string AvailityContainerName { get; init; } = "availity";

    /// <summary>
    /// Container for patient invoice PDF artifacts.
    /// </summary>
    public string InvoicePdfContainerName { get; init; } = "rtainvoices";

    /// <summary>
    /// Maximum number of retry attempts for transient blob upload failures.
    /// </summary>
    public int MaxUploadRetries { get; init; } = 3;

    /// <summary>
    /// Initial delay in milliseconds for exponential back-off on upload retry.
    /// </summary>
    public int RetryDelayMs { get; init; } = 500;
}
