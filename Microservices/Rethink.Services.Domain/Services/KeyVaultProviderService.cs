using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Rethink.Services.Domain.Interfaces;

/// <summary>
/// Key Vault accessor with short in-memory TTL to avoid per-request vault round-trips (thread-pool friendly).
/// </summary>
public class KeyVaultProviderService : IKeyVaultProviderService
{
    private readonly SecretClient _secretClient;
    private readonly TimeSpan _cacheTtl;
    private readonly Dictionary<string, (string Value, DateTimeOffset ExpiresUtc)> _cache = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _cacheGate = new(1, 1);

    public KeyVaultProviderService(IConfiguration configuration)
    {
        string keyVaultUrl = configuration["KeyVaultUri"];
        _secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
        var minutes = 5;
        if (int.TryParse(configuration["KeyVault:SecretCacheMinutes"], out var m) && m > 0 && m <= 60)
        {
            minutes = m;
        }

        _cacheTtl = TimeSpan.FromMinutes(minutes);
    }

    public async Task<string?> GetSecretAsync(string secretName)
    {
        if (string.IsNullOrEmpty(secretName))
        {
            return null;
        }

        await _cacheGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_cache.TryGetValue(secretName, out var entry) && entry.ExpiresUtc > DateTimeOffset.UtcNow)
            {
                return entry.Value;
            }

            KeyVaultSecret secret = await _secretClient.GetSecretAsync(secretName).ConfigureAwait(false);
            var value = secret.Value;
            _cache[secretName] = (value, DateTimeOffset.UtcNow.Add(_cacheTtl));
            return value;
        }
        finally
        {
            _cacheGate.Release();
        }
    }
}