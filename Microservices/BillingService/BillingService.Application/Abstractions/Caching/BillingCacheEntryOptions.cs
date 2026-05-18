namespace BillingService.Application.Abstractions.Caching;

public sealed record BillingCacheEntryOptions
{
    public static readonly BillingCacheEntryOptions DashboardSummary = new(TimeSpan.FromMinutes(2), true);
    public static readonly BillingCacheEntryOptions BillingMetrics = new(TimeSpan.FromMinutes(5), true);
    public static readonly BillingCacheEntryOptions InvoiceSummary = new(TimeSpan.FromMinutes(3), true);
    public static readonly BillingCacheEntryOptions LookupData = new(TimeSpan.FromHours(6), false);
    public static readonly BillingCacheEntryOptions SearchResults = new(TimeSpan.FromMinutes(1), true);
    public static readonly BillingCacheEntryOptions UserPermissions = new(TimeSpan.FromMinutes(10), true);

    public BillingCacheEntryOptions(TimeSpan ttl, bool tenantScoped)
    {
        if (ttl <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl), "TTL must be greater than zero.");
        }

        Ttl = ttl;
        TenantScoped = tenantScoped;
    }

    public TimeSpan Ttl { get; }

    public bool TenantScoped { get; }
}
