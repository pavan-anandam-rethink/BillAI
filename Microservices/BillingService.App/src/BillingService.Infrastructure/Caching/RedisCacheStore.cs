using BillingService.App.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace BillingService.App.Infrastructure.Caching;

public sealed class RedisCacheStore : ICacheStore
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<RedisCacheStore> _logger;

    public RedisCacheStore(IDistributedCache distributedCache, ILogger<RedisCacheStore> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public Task<string?> GetStringAsync(string key, CancellationToken cancellationToken)
    {
        return _distributedCache.GetStringAsync(key, cancellationToken);
    }

    public Task SetStringAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken)
    {
        return _distributedCache.SetStringAsync(
            key,
            value,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            },
            cancellationToken);
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "RemoveByPrefix called for {Prefix}. Prefix scans are intentionally disabled in production-safe mode.",
            prefix);
        return Task.CompletedTask;
    }
}

