namespace BillingService.LegacyAdapters;

public sealed record LegacyAdapterResult<T>(T Value, string CompatibilityBoundary)
{
    public static LegacyAdapterResult<T> FromLegacyService(T value, string boundary)
    {
        if (string.IsNullOrWhiteSpace(boundary))
        {
            boundary = "BillingService.Legacy";
        }

        return new LegacyAdapterResult<T>(value, boundary);
    }
}
