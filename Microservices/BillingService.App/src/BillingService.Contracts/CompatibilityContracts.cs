namespace BillingService.App.Contracts;

public sealed record LegacyJsonPayload(string Json);

public sealed record LegacyRoute(string Path, HttpMethod Method)
{
    public static LegacyRoute Post(string path) => new(path, HttpMethod.Post);
    public static LegacyRoute Get(string path) => new(path, HttpMethod.Get);
}

public static class KnownBillingRoutes
{
    public static readonly LegacyRoute ClaimGetClaimHeaders = LegacyRoute.Post("Claim/GetClaimHeaders");
    public static readonly LegacyRoute ClaimGetClaimsCount = LegacyRoute.Post("Claim/GetClaimsCount");
    public static readonly LegacyRoute DashboardPatientInvoices = LegacyRoute.Post("PatientInvoice/GetAllPendingCollectionData");
}

