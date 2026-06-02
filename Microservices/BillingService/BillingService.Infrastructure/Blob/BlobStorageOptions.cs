namespace BillingService.Infrastructure.Blob;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BillingService:BlobStorage";

    /// <summary>
    /// Azure Blob Storage connection string. Must be provided via Key Vault
    /// or Kubernetes Secret — never stored in application config files.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;
}
