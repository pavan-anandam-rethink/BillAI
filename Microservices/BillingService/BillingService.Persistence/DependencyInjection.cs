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
        services.AddScoped<IOutboxWriter, SqlOutboxWriter>();
        services.AddScoped<IOutboxReader, SqlOutboxReader>();

        return services;
    }
}
