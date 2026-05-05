using System.Threading.Tasks;
using System.Threading;
using System;
using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Azure.Identity;
using Rethink.Services.Domain.Interfaces;


public class KeyVaultProviderService : IKeyVaultProviderService
{
    private readonly SecretClient _secretClient;

    public KeyVaultProviderService(IConfiguration configuration)
    {
        string keyVaultUrl = configuration["KeyVaultUri"];
        _secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
    }

    public async Task<string?> GetSecretAsync(string secretName)
    {
        KeyVaultSecret secret = await _secretClient.GetSecretAsync(secretName);
        return secret.Value;
    }
}