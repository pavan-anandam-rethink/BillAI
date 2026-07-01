using ClaimLifecycleMonitoring.Application.Abstractions.Persistence;
using ClaimLifecycleMonitoring.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimLifecycleMonitoring.Persistence.DependencyInjection;

/// <summary>DI helpers for the Persistence layer.</summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>The default configuration key holding the SQL Server connection string.</summary>
    public const string ConnectionStringName = "ClaimLifecycleDb";

    /// <summary>Registers the EF Core <see cref="ClaimLifecycleDbContext"/>, repositories and unit of work.</summary>
    public static IServiceCollection AddClaimLifecyclePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connection = configuration.GetConnectionString(ConnectionStringName);
        var useInMemory = string.IsNullOrWhiteSpace(connection)
            || string.Equals(configuration["Persistence:UseInMemory"], "true", StringComparison.OrdinalIgnoreCase);

        services.AddDbContext<ClaimLifecycleDbContext>(options =>
        {
            if (useInMemory)
            {
                options.UseInMemoryDatabase("ClaimLifecycleMonitoring");
            }
            else
            {
                options.UseSqlServer(connection, sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", ClaimLifecycleDbContext.Schema);
                    sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(15), errorNumbersToAdd: null);
                });
            }
            options.EnableDetailedErrors();
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ClaimLifecycleDbContext>());
        services.AddScoped<IClaimRepository, ClaimRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IStageSlaConfigurationRepository, StageSlaConfigurationRepository>();

        return services;
    }
}
