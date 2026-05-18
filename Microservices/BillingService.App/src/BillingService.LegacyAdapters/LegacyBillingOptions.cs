namespace BillingService.App.LegacyAdapters;

public sealed class LegacyBillingOptions
{
    public const string SectionName = "LegacyBilling";

    public string BaseUrl { get; init; } = "http://billingservice-web";
    public int TimeoutSeconds { get; init; } = 30;
}

