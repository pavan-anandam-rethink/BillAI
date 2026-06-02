namespace BillingService.Application.Common.Configuration;

public sealed class ModernizationFeatureFlags
{
    public const string SectionName = "BillingService:Modernization";

    public bool EnableCleanArchitectureAdapters { get; init; }

    public bool EnableDistributedCacheDecorators { get; init; }

    public bool EnableOutboxPublisher { get; init; }

    public bool EnableReadModelQueries { get; init; }

    /// <summary>
    /// When true, registers <c>IBlobStore</c> backed by Azure Blob Storage via the
    /// Infrastructure layer. Requires <c>BillingService:BlobStorage:ConnectionString</c>
    /// to be provided through Key Vault or Kubernetes Secrets.
    /// </summary>
    public bool EnableBlobStore { get; init; }
}
