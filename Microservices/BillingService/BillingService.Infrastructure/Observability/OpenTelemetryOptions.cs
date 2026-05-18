namespace BillingService.Infrastructure.Observability;

public sealed class OpenTelemetryOptions
{
    public const string SectionName = "BillingService:OpenTelemetry";

    public string ServiceName { get; init; } = "BillingService";

    public string ServiceVersion { get; init; } = "1.0.0";

    public string? OtlpEndpoint { get; init; }
}
