using Azure.Messaging.ServiceBus;
using BillingService.App.Application.Abstractions;
using BillingService.App.Infrastructure.Caching;
using BillingService.App.Infrastructure.Messaging;
using BillingService.App.Infrastructure.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BillingService.App.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
                                    ?? configuration["RedisCache:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "billing:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddScoped<ICacheStore, RedisCacheStore>();
        services.AddBillingObservability(configuration);

        var serviceBusConnection = configuration.GetConnectionString("ServiceBus")
                                   ?? configuration["ConnectionStrings:ServiceBus:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(serviceBusConnection))
        {
            services.AddSingleton(new ServiceBusClient(serviceBusConnection));
            services.AddScoped<ServiceBusOutboxDispatcher>();
        }

        return services;
    }
}

