using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StediIntegration.Infrastructure.Persistence;

namespace StediIntegration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<StediIntegrationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(StediIntegrationDbContext).Assembly.FullName)));

        services.AddScoped<StediIntegration.Domain.Interfaces.IStediTransactionRepository, StediTransactionRepository>();
        return services;
    }
}
