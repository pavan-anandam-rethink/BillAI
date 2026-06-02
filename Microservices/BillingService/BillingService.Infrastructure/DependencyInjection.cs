using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using BillingService.Application.Abstractions.Blob;
using BillingService.Application.Abstractions.Caching;
using BillingService.Application.Abstractions.Clock;
using BillingService.Application.Abstractions.Messaging;
using BillingService.Infrastructure.Blob;
using BillingService.Infrastructure.Caching;
using BillingService.Infrastructure.Messaging;
using BillingService.Infrastructure.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BillingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool enableDistributedCache = false,
        bool enableEventBus = false,
        bool enableBlobStore = false,
        bool enableOpenTelemetry = true)
    {
        services.Configure<RedisCacheOptions>(configuration.GetSection(RedisCacheOptions.SectionName));
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.Configure<BlobStorageOptions>(configuration.GetSection(BlobStorageOptions.SectionName));

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        if (enableDistributedCache)
        {
            var redisOptions = configuration.GetSection(RedisCacheOptions.SectionName).Get<RedisCacheOptions>()
                ?? new RedisCacheOptions();

            if (string.IsNullOrWhiteSpace(redisOptions.Configuration))
            {
                throw new InvalidOperationException(
                    $"{RedisCacheOptions.SectionName}:Configuration is required when distributed cache is enabled.");
            }

            services.AddScoped<IBillingCache, DistributedBillingCache>();
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisOptions.Configuration;
                options.InstanceName = redisOptions.InstanceName;
            });
        }

        if (enableEventBus)
        {
            var serviceBusOptions = configuration.GetSection(ServiceBusOptions.SectionName).Get<ServiceBusOptions>()
                ?? new ServiceBusOptions();

            if (string.IsNullOrWhiteSpace(serviceBusOptions.ConnectionString))
            {
                throw new InvalidOperationException(
                    $"{ServiceBusOptions.SectionName}:ConnectionString is required when event bus publishing is enabled.");
            }

            services.AddSingleton<IEventBus, AzureServiceBusEventBus>();
            services.AddSingleton(_ => new ServiceBusClient(serviceBusOptions.ConnectionString));
        }

        if (enableBlobStore)
        {
            var blobOptions = configuration.GetSection(BlobStorageOptions.SectionName).Get<BlobStorageOptions>()
                ?? new BlobStorageOptions();

            if (string.IsNullOrWhiteSpace(blobOptions.ConnectionString))
            {
                throw new InvalidOperationException(
                    $"{BlobStorageOptions.SectionName}:ConnectionString is required when blob store is enabled.");
            }

            services.AddSingleton(_ => new BlobServiceClient(blobOptions.ConnectionString));
            services.AddScoped<IBlobStore, AzureBlobStore>();
        }

        if (enableOpenTelemetry)
        {
            services.AddBillingOpenTelemetry(configuration);
        }

        return services;
    }
}

internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
