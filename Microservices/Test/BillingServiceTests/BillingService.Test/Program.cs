using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Rethink.Services.Common.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BillingService.Test
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            new HostBuilder()
                .ConfigureLogging(logBuilder => LogConfigHelper.ConfigureWorkerLogging(logBuilder))
                .ConfigureHostConfiguration(builder => // setup your app's configuration
                {
                    var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                    builder
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile($"appsettings.{environmentName}.json", true, true)
                        .AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) => // configure DI, including the actual background services
                    new ServicesConfiguration(services, hostContext.Configuration).Configure());
    }
}