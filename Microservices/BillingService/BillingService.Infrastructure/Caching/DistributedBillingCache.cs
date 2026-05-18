using BillingService.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BillingService.Infrastructure.Caching;

public sealed class DistributedBillingCache(
    IDistributedCache distributedCache,
    ILogger<DistributedBillingCache> logger)
    : IBillingCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var payload = await distributedCache.GetStringAsync(key, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(payload, SerializerOptions);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Failed to deserialize BillingService cache entry {CacheKey}", key);
            await RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            return default;
        }
    }

    public Task SetAsync<T>(
        string key,
        T value,
        BillingCacheEntryOptions options,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = options.Ttl
        };

        return distributedCache.SetStringAsync(key, payload, cacheOptions, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return distributedCache.RemoveAsync(key, cancellationToken);
    }
}
