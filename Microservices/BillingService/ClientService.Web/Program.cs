using ClientService.Web.infrastructure;
using Rethink.Services.Common.Helpers;
using Rethink.Services.Domain.Interfaces;
using RethinkCore.Common.Logging.Extensions;

namespace ClientService.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(builder =>
                {
                    var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                    builder
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile($"appsettings.{environmentName}.json", true, true)
                        .AddEnvironmentVariables();
                })
                .ConfigureLogging((context, loggingBuilder) =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddConsole();
                    loggingBuilder.AddDebug();

                    loggingBuilder.AddAppInsightLogger(context.Configuration);

                    LogConfigHelper.ConfigureApiLogging(loggingBuilder);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IKeyVaultProviderService, KeyVaultProviderService>();

                    using var provider = services.BuildServiceProvider();
                    var keyVaultProvider = provider.GetRequiredService<IKeyVaultProviderService>();

                    var appInsightsSecretName = context.Configuration["ApplicationInsights:APPLICATIONINSIGHTS_CONNECTION_STRING"];
                    var appInsightsConnectionString = keyVaultProvider.GetSecretAsync(appInsightsSecretName).Result;

                    services.AddApplicationInsightsTelemetry(options =>
                    {
                        options.ConnectionString = appInsightsConnectionString;
                    });

                    services.AddRethinkLogging(context.Configuration);
                    services.AddInfrastructure(context.Configuration);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
