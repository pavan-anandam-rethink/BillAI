using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace IdentityService.Infrastructure.Logging;

public static class SerilogConfiguration
{
    public static IHostBuilder UseSerilogLogging(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs/identity-service-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30);
        });
    }
}
