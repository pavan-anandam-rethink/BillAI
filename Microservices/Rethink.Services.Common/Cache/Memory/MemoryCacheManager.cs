using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Cache.Memory
{
    [ExcludeFromCodeCoverage]
    public class MemoryCacheManager(IMemoryCache cache, IConfiguration configuration) : IMemoryCacheManager
    {
        private readonly IMemoryCache _memoryCache = cache ?? throw new ArgumentNullException(nameof(cache));
        private readonly string _cacheScope = configuration.GetSection("CacheSettings:CacheScope").Value ?? string.Empty;

        private string GetScopedCacheKey(string key) => $"{_cacheScope}:{key}";

        public async Task<T> GetAsync<T>(string key, Func<Task<T>> acquire, CachingDuration duration)
        {
            var scopedKey = GetScopedCacheKey(key);
            if (_memoryCache.TryGetValue(scopedKey, out T value))
                return value;

            var result = await acquire();

            await SetAsync(scopedKey, result, duration);

            return result;
        }

        public async Task SetAsync(string key, object data, CachingDuration duration)
        {
            var scopedKey = GetScopedCacheKey(key);
            if (data != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds((int)duration));
                _memoryCache.Set(scopedKey, data, cacheEntryOptions);
            }

            await Task.CompletedTask;
        }

        public async Task Remove(string key)
        {
            var scopedKey = GetScopedCacheKey(key);
            _memoryCache.Remove(scopedKey);

            await Task.CompletedTask;
        }

        public async Task Clear()
        {
            if (_memoryCache is MemoryCache memoryCache)
            {
                var percentage = 1.0; //100%
                memoryCache.Compact(percentage);
            }

            await Task.CompletedTask;
        }
    }
}
