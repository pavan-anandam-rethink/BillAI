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
            // The OutboxPublisherWorker resolves IOutboxPoller per-batch via IServiceScopeFactory.
            // Ensure IOutboxPoller (and its dependencies) is registered before calling this method.
            services.AddHostedService<OutboxPublisherWorker>();
        }

        return services;
    }
}
