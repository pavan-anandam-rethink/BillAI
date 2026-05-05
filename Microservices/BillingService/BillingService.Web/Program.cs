using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rethink.Services.Common.Helpers;
using Rethink.Services.Domain.Interfaces;
using RethinkCore.Common.Logging.Extensions;

namespace BillingService.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
        builder.Logging.AddAppInsightLogger(builder.Configuration);
        LogConfigHelper.ConfigureApiLogging(builder.Logging);

        IKeyVaultProviderService keyVault = builder.Environment.IsEnvironment(IntegrationTestKeyVaultProvider.EnvironmentName)
            ? new IntegrationTestKeyVaultProvider()
            : new KeyVaultProviderService(builder.Configuration);

        if (keyVault is KeyVaultProviderService concreteKv)
        {
            builder.Services.AddSingleton(concreteKv);
            builder.Services.AddSingleton<IKeyVaultProviderService>(sp => sp.GetRequiredService<KeyVaultProviderService>());
        }
        else
        {
            builder.Services.AddSingleton(keyVault);
        }

        await BillingWebHostBootstrap.AddServicesAsync(builder, keyVault).ConfigureAwait(false);

        var app = builder.Build();
        BillingWebHostBootstrap.ConfigurePipeline(app);

        await app.RunAsync().ConfigureAwait(false);
    }
}
