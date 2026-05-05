using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace BillingService.Web;

/// <summary>Registers Application Insights only when Key Vault yields a connection string (avoids double registration).</summary>
public sealed class ApplicationInsightsStarter
{
    private static bool Registered;

    public void TryAddIfConfigured(string? connectionString, IServiceCollection services)
    {
        if (Registered || string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
        {
            ConnectionString = connectionString
        });
        Registered = true;
    }
}
