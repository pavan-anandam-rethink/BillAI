using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SftpIngestion.Domain.Interfaces;
using SftpIngestion.Infrastructure.Persistence;
using SftpIngestion.Infrastructure.Sftp;

namespace SftpIngestion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SftpIngestionDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(SftpIngestionDbContext).Assembly.FullName)));

        services.AddScoped<IIngestedFileRepository, IngestedFileRepository>();
        services.AddSingleton<ISftpClientFactory, SshNetSftpClientFactory>();

        return services;
    }
}
