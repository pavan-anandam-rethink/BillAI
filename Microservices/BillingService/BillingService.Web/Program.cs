using BillingService.Web.Servers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Helpers;
using Rethink.Services.Domain.Interfaces;
using RethinkCore.Common.Logging.Extensions;
using System;
using System.IO;

namespace BillingService.Web
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

                    using var serviceProvider = services.BuildServiceProvider();
                    var secretProvider = serviceProvider.GetRequiredService<IKeyVaultProviderService>();

                    var appInsightsConnStringSecretKey =
                        context.Configuration["ApplicationInsights:APPLICATIONINSIGHTS_CONNECTION_STRING"];

                    var appInsightsConnectionString =
                        secretProvider.GetSecretAsync(appInsightsConnStringSecretKey).Result;
                    services.AddApplicationInsightsTelemetry(options =>
                    {
                        options.ConnectionString = appInsightsConnectionString;
                    });
                    services.AddRethinkLogging(context.Configuration);
                    services.AddSingleton<IPusherNotificationServer, PusherNotificationServer>();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}   