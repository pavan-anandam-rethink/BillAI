namespace BillingService.App.Domain;

public sealed class BillingModernizationSettings
{
    public const string SectionName = "BillingModernization";

    public bool UseLegacyProxyFallback { get; init; } = true;
    public MigrationMode ClaimHeadersMigrationMode { get; init; } = MigrationMode.Off;
    public bool UseCqrsDashboard { get; init; } = false;
    public int DashboardSummaryTtlSeconds { get; init; } = 120;
    public int ClaimHeaderTtlSeconds { get; init; } = 30;
    public bool ExposeModernizationStatusEndpoint { get; init; } = false;
}

public enum MigrationMode
{
    Off = 0,
    Shadow = 1,
    On = 2
}

