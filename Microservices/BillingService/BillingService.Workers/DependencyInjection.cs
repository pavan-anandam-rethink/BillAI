using BillingService.Workers.Outbox;
using Microsoft.Extensions.DependencyInjection;

namespace BillingService.Workers;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingWorkers(
        this IServiceCollection services,
        bool enableOutboxPublisher = false)
    {
        if (enableOutboxPublisher)
        {
            services.AddHostedService<OutboxPublisherWorker>();
        }

        return services;
    }
}
