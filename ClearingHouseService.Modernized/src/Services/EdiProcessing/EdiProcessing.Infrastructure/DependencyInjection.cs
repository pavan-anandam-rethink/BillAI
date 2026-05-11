using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EdiProcessing.Infrastructure.Persistence;

namespace EdiProcessing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EdiProcessingDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(EdiProcessingDbContext).Assembly.FullName)));

        services.AddScoped<EdiProcessing.Domain.Interfaces.IEdiParser, Parsers.BasicEdiParser>();
        services.AddScoped<EdiProcessing.Domain.Interfaces.IEdiValidator, Parsers.BasicEdiValidator>();
        return services;
    }
}
