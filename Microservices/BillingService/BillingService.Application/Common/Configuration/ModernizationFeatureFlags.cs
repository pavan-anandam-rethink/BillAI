namespace BillingService.Application.Common.Configuration;

public sealed class ModernizationFeatureFlags
{
    public const string SectionName = "BillingService:Modernization";

    public bool EnableCleanArchitectureAdapters { get; init; }

    public bool EnableDistributedCacheDecorators { get; init; }

    public bool EnableOutboxPublisher { get; init; }

    public bool EnableReadModelQueries { get; init; }

    /// <summary>
    /// Enables the blob-first storage architecture: IBlobStorageService and IBlobMetadataStore
    /// are registered and the Azure Blob Storage implementation is used for all artifact storage.
    /// When false, the legacy IBillingBlobService remains the active implementation.
    /// </summary>
    public bool EnableBlobFirstStorage { get; init; }
}
