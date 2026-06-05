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
        string billingConnectionString,
        bool enableOutbox = false,
        bool enableBlobMetadata = false)
    {
        services.AddScoped<IUnitOfWork, BillingDbUnitOfWork>();
        services.AddSingleton<IBillingSqlConnectionFactory>(_ => new BillingSqlConnectionFactory(billingConnectionString));

        if (enableOutbox)
        {
            services.AddScoped<IOutboxWriter, OutboxPersistenceWriter>();
            services.AddScoped<IOutboxPoller, SqlOutboxPoller>();
        }

        if (enableBlobMetadata)
        {
            services.AddScoped<IBlobMetadataStore, SqlBlobMetadataStore>();
        }

        return services;
    }
}
