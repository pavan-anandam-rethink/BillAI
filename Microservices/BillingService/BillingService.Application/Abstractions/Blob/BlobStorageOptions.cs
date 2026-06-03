namespace BillingService.Application.Abstractions.Blob;

/// <summary>
/// Configuration options for blob-first storage architecture.
/// Connection string is resolved from Key Vault; never stored in config files.
/// </summary>
public sealed class BlobStorageOptions
{
    public const string SectionName = "BillingService:BlobStorage";

    /// <summary>
    /// Azure Blob Storage connection string.
    /// Resolved from Key Vault at runtime; leave blank in config and inject via Key Vault secret reference.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>Default container for EDI request files (837/270).</summary>
    public string EdiRequestContainer { get; init; } = "edi-requests";

    /// <summary>Default container for EDI response files (835/277/271/999).</summary>
    public string EdiResponseContainer { get; init; } = "edi-responses";

    /// <summary>Default container for clearinghouse acknowledgement files.</summary>
    public string AcknowledgementContainer { get; init; } = "clearinghouse-acks";

    /// <summary>Default container for general billing artifacts.</summary>
    public string BillingArtifactsContainer { get; init; } = "billing-artifacts";
}
