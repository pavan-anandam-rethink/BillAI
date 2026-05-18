using BillingService.Application.Abstractions.Caching;
using BillingService.Application.Common.Behaviors;
using BillingService.Application.Common.Configuration;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BillingService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ModernizationFeatureFlags>(
            configuration.GetSection(ModernizationFeatureFlags.SectionName));

        services.AddSingleton<ICacheKeyBuilder, CacheKeyBuilder>();
        services.AddMediatR(options => options.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceLoggingBehavior<,>));

        return services;
    }
}
