// Infrastructure/KeyVault/KeyVaultServiceCollectionExtensions.cs
using System;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using BillingService.Domain.Interfaces.Provider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class KeyVaultServiceCollectionExtensions
{
    public static IServiceCollection AddKeyVaultSecretProvider(this IServiceCollection services, IConfiguration configuration)
    {
        //services.AddSingleton<TokenCredential>(_ => new DefaultAzureCredential());

        //services.AddSingleton(sp =>
        //{
        //    var cfg = sp.GetRequiredService<IConfiguration>();
        //    var vaultUri = cfg["KeyVaultUri"]
        //                   ?? throw new InvalidOperationException("KeyVaultUri:VaultUri missing");
        //    var credential = sp.GetRequiredService<TokenCredential>();
        //    return new SecretClient(new Uri(vaultUri), credential);
        //});

        //services.AddSingleton<IKeyVaultProviderService, KeyVaultProviderService>();
        return null;
        //return services;
    }
}
