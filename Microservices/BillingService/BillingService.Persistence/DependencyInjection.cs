using BillingService.Application.Abstractions.Blob;
using BillingService.Application.Abstractions.Messaging;
using BillingService.Application.Abstractions.Persistence;
using BillingService.Persistence.Blob;
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

        return services;
    }

    public static IServiceCollection AddBillingOutbox(this IServiceCollection services)
    {
        services.AddScoped<IOutboxWriter, OutboxPersistenceWriter>();
        services.AddScoped<IOutboxPoller, SqlOutboxPoller>();
        return services;
    }

    public static IServiceCollection AddBillingBlobMetadataStore(this IServiceCollection services)
    {
        services.AddScoped<IBlobMetadataStore, SqlBlobMetadataStore>();
        return services;
    }
}
