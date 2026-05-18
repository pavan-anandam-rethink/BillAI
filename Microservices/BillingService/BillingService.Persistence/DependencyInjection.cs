using BillingService.Application.Abstractions.Persistence;
using BillingService.Persistence.Legacy;
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
}
