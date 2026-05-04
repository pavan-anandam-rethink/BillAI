using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Cache.Redis
{
    public class RedisCacheManager(IConnectionMultiplexer connectionMultiplexer, IConfiguration configuration) : IRedisCacheManager
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        private readonly string _cacheScope = configuration.GetSection("CacheSettings:CacheScope").Value ?? string.Empty;

        private string GetScopedCacheKey(string key) => $"{_cacheScope}:{key}";


        public async Task SetAsync(string key, object data, CachingDuration duration)
        {
            var scopedKey = GetScopedCacheKey(key);

            if (data != null)
            {
                var jsonString = JsonConvert.SerializeObject(data);

                var db = _connectionMultiplexer.GetDatabase();

                await db.StringSetAsync(scopedKey, jsonString, TimeSpan.FromSeconds((int)duration));
            }
        }
        public async Task<T> GetAsync<T>(string key, Func<Task<T>> acquire, CachingDuration duration)
        {
            var scopedKey = GetScopedCacheKey(key);

            var db = _connectionMultiplexer.GetDatabase();

            var cachedObject = await db.StringGetAsync(scopedKey);

            if (!string.IsNullOrEmpty(cachedObject))
            {
                return JsonConvert.DeserializeObject<T>(cachedObject);
            }

            var data = await acquire();

            await SetAsync(scopedKey, data, duration);

            return data;
        }

        public async Task Remove(string key)
        {
            var scopedKey = GetScopedCacheKey(key);
            var db = _connectionMultiplexer.GetDatabase();

            await db.KeyDeleteAsync(scopedKey);
        }

        public async Task Clear()
        {
            var endpoints = _connectionMultiplexer.GetEndPoints(true);
            foreach (var endpoint in endpoints)
            {
                var server = _connectionMultiplexer.GetServer(endpoint);
                await server.FlushAllDatabasesAsync();
            }
        }
    }
}
