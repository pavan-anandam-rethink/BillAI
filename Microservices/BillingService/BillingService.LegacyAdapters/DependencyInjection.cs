using BillingService.LegacyAdapters.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace BillingService.LegacyAdapters;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingLegacyAdapters(this IServiceCollection services)
    {
        services.AddScoped<IClaimCompatibilityFacade, ClaimCompatibilityFacade>();
        return services;
    }
}
