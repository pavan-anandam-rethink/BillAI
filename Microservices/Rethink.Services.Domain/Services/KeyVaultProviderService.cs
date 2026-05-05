using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Rethink.Services.Domain.Interfaces;

/// <summary>
/// Key Vault secret access with short in-process TTL caching to cut latency under load
/// (repeated secret names coalesce while TTL is valid; concurrent cold requests serialize per key).
/// </summary>
public class KeyVaultProviderService : IKeyVaultProviderService
{
    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromMinutes(15);

    private readonly SecretClient _secretClient;
    private readonly TimeSpan _cacheTtl;

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();

    private sealed class CacheEntry
    {
        public CacheEntry(string? value, DateTime expiresUtc)
        {
            Value = value;
            ExpiresUtc = expiresUtc;
        }

        public string? Value { get; }
        public DateTime ExpiresUtc { get; }
    }

    public KeyVaultProviderService(IConfiguration configuration, TimeSpan? cacheTtl = null)
    {
        string keyVaultUrl = configuration["KeyVaultUri"]
            ?? throw new InvalidOperationException("Configuration KeyVaultUri is required.");
        _secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
        _cacheTtl = cacheTtl ?? DefaultCacheTtl;
    }

    public async Task<string> GetSecretAsync(string secretName)
    {
        if (string.IsNullOrEmpty(secretName))
        {
            return string.Empty;
        }

        var now = DateTime.UtcNow;
        if (_cache.TryGetValue(secretName, out var entry) && entry.ExpiresUtc > now)
        {
            return entry.Value ?? string.Empty;
        }

        var gate = _keyLocks.GetOrAdd(secretName, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync().ConfigureAwait(false);

        try
        {
            now = DateTime.UtcNow;
            if (_cache.TryGetValue(secretName, out entry) && entry!.ExpiresUtc > now)
            {
                return entry.Value ?? string.Empty;
            }

            KeyVaultSecret secret = await _secretClient.GetSecretAsync(secretName).ConfigureAwait(false);
            var value = secret.Value ?? string.Empty;
            _cache[secretName] = new CacheEntry(value, DateTime.UtcNow.Add(_cacheTtl));
            return value;
        }
        finally
        {
            gate.Release();
        }
    }
}
