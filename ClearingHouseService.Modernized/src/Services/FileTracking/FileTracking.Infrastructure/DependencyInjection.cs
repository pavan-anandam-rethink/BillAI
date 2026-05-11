using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FileTracking.Infrastructure.Persistence;

namespace FileTracking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FileTrackingDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(FileTrackingDbContext).Assembly.FullName)));

        services.AddScoped<FileTracking.Domain.Interfaces.IFileTrackingRepository, FileTrackingRepository>();
        return services;
    }
}
