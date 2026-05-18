namespace BillingService.Application.Common.Configuration;

public sealed class ModernizationFeatureFlags
{
    public const string SectionName = "BillingService:Modernization";

    public bool EnableCleanArchitectureAdapters { get; init; }

    public bool EnableDistributedCacheDecorators { get; init; }

    public bool EnableOutboxPublisher { get; init; }

    public bool EnableReadModelQueries { get; init; }
}
