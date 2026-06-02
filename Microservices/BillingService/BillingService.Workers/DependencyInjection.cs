using BillingService.Workers.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BillingService.Workers;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingWorkers(
        this IServiceCollection services,
        IConfiguration configuration,
        bool enableOutboxPublisher = false)
    {
        services.Configure<OutboxPublisherOptions>(
            configuration.GetSection(OutboxPublisherOptions.SectionName));

        if (enableOutboxPublisher)
        {
            services.AddHostedService<OutboxPublisherWorker>();
        }

        return services;
    }
}
