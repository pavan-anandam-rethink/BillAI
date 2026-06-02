namespace BillingService.Infrastructure.BlobStorage;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BillingService:BlobStorage";

    /// <summary>
    /// Azure Blob Storage connection string or managed-identity endpoint.
    /// Resolved from Key Vault at startup; never stored in config files.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;
}
