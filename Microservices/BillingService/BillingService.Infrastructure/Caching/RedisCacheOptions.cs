namespace BillingService.Infrastructure.Caching;

public sealed class RedisCacheOptions
{
    public const string SectionName = "BillingService:Redis";

    public string Configuration { get; init; } = string.Empty;

    public string InstanceName { get; init; } = "billing:";
}
