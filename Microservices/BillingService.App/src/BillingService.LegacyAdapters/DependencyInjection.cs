using BillingService.App.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BillingService.App.LegacyAdapters;

public static class DependencyInjection
{
    public static IServiceCollection AddLegacyBillingAdapters(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LegacyBillingOptions>(configuration.GetSection(LegacyBillingOptions.SectionName));
        var options = configuration.GetSection(LegacyBillingOptions.SectionName).Get<LegacyBillingOptions>() ?? new LegacyBillingOptions();

        services.AddHttpClient("legacy-billing", client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds));
        })
        .AddStandardResilienceHandler();

        services.AddScoped<ILegacyBillingGateway, HttpLegacyBillingGateway>();
        return services;
    }
}

