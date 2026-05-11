using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reconciliation.Infrastructure.Persistence;

namespace Reconciliation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ReconciliationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ReconciliationDbContext).Assembly.FullName)));

        services.AddScoped<Reconciliation.Domain.Interfaces.IReconciliationRepository, ReconciliationRepository>();
        return services;
    }
}
