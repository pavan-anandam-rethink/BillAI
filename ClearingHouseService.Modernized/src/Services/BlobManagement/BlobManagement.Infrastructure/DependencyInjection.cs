using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BlobManagement.Infrastructure.Persistence;

namespace BlobManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BlobManagementDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(BlobManagementDbContext).Assembly.FullName)));

        services.AddScoped<BlobManagement.Domain.Interfaces.IBlobFileRepository, BlobFileRepository>();
        return services;
    }
}
