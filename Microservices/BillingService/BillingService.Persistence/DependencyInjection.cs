using BillingService.Application.Abstractions.Messaging;
using BillingService.Application.Abstractions.Persistence;
using BillingService.Persistence.Legacy;
using BillingService.Persistence.Outbox;
using Microsoft.Extensions.DependencyInjection;

namespace BillingService.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingPersistenceCompatibility(
        this IServiceCollection services,
        string billingConnectionString)
    {
        services.AddScoped<IUnitOfWork, BillingDbUnitOfWork>();
        services.AddSingleton<IBillingSqlConnectionFactory>(_ => new BillingSqlConnectionFactory(billingConnectionString));

        // Durable outbox – write path
        services.AddScoped<IOutboxWriter, OutboxPersistenceWriter>();

        // Durable outbox – poll path (singleton-safe: each method opens its own connection)
        services.AddSingleton<IOutboxPoller, SqlOutboxPoller>();

        return services;
    }
}
