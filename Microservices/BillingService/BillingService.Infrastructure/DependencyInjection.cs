using Azure.Messaging.ServiceBus;
using BillingService.Application.Abstractions.Caching;
using BillingService.Application.Abstractions.Clock;
using BillingService.Application.Abstractions.Messaging;
using BillingService.Infrastructure.Caching;
using BillingService.Infrastructure.Messaging;
using BillingService.Infrastructure.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BillingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RedisCacheOptions>(configuration.GetSection(RedisCacheOptions.SectionName));
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IBillingCache, DistributedBillingCache>();
        services.AddSingleton<IEventBus, AzureServiceBusEventBus>();
        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
            return new ServiceBusClient(options.ConnectionString);
        });

        services.AddStackExchangeRedisCache(options =>
        {
            var redisOptions = configuration.GetSection(RedisCacheOptions.SectionName).Get<RedisCacheOptions>()
                ?? new RedisCacheOptions();

            options.Configuration = redisOptions.Configuration;
            options.InstanceName = redisOptions.InstanceName;
        });

        services.AddBillingOpenTelemetry(configuration);

        return services;
    }
}

internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
