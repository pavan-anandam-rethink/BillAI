using BillingService.Domain.Interfaces.Common;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Common
{
    public class CacheService :ICacheService
    {
        private readonly IDistributedCache _redisCache;
        private readonly ILogger<CacheService> _logger;
        public CacheService(IDistributedCache redisCache, ILogger<CacheService> logger)
        {
            _redisCache = redisCache;
            _logger = logger;
        }
        public async Task<T> GetOrSetCacheAsync<T>(string cacheKey, Func<Task<T>> fetchDataFunc, TimeSpan expirationTime)
        {
            try
            {
                var cachedData = await _redisCache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    return JsonConvert.DeserializeObject<T>(cachedData);
                }                
                var freshData = await fetchDataFunc();
                if (freshData != null)
                {
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = expirationTime
                    };
                    await _redisCache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(freshData), cacheOptions);
                }
                return freshData;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in caching: {ex.Message}");
                throw;
            }
        }

        public async Task RemoveAsync(string cacheKey)
        {
            try
            {
                await _redisCache.RemoveAsync(cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache key {Key}", cacheKey);               
            }
        }
    }
}
