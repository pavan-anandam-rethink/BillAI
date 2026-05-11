using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FileMetadata.Infrastructure.Persistence;

namespace FileMetadata.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FileMetadataDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(FileMetadataDbContext).Assembly.FullName)));

        services.AddScoped<FileMetadata.Domain.Interfaces.IFileMetadataRepository, FileMetadataRepository>();
        services.AddScoped<FileMetadata.Domain.Interfaces.IFileEventHistoryRepository, FileEventHistoryRepository>();
        return services;
    }
}
