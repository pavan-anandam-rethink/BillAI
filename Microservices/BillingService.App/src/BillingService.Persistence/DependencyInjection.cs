using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BillingService.App.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BillingOutbox")
                               ?? configuration.GetConnectionString("Database")
                               ?? throw new InvalidOperationException("Connection string 'BillingOutbox' or 'Database' is required.");

        services.AddDbContext<BillingOutboxDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null);
            });
        });

        services.AddScoped<IOutboxRepository, OutboxRepository>();
        return services;
    }
}

