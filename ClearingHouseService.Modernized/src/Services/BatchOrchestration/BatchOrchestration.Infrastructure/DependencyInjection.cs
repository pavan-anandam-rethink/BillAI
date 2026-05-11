using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BatchOrchestration.Infrastructure.Persistence;

namespace BatchOrchestration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BatchOrchestrationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(BatchOrchestrationDbContext).Assembly.FullName)));

        services.AddScoped<BatchOrchestration.Domain.Interfaces.IBatchRepository, BatchRepository>();
        return services;
    }
}
