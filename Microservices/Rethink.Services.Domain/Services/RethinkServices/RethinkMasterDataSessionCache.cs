using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Domain.Services.RethinkServices
{
    /// <summary>
    /// Redis-backed cache keyed by billing session + account + upstream path hash.
    /// </summary>
    public sealed class RethinkMasterDataSessionCache : IRethinkMasterDataSessionCache
    {
        private static readonly ActivitySource ActivitySource = new("Rethink.Billing.MasterDataSession");

        private readonly IConnectionMultiplexer _redis;
        private readonly string _keyPrefix;
        private readonly TimeSpan _ttl;
        private readonly ILogger<RethinkMasterDataSessionCache>? _logger;
        private static readonly JsonSerializerSettings SerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        private const string NullMarker = "__bh_null__";

        public RethinkMasterDataSessionCache(IConnectionMultiplexer redis, IConfiguration configuration, ILogger<RethinkMasterDataSessionCache>? logger = null)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            var section = configuration.GetSection("RethinkMasterDataSession");
            _keyPrefix = section["KeyPrefix"] ?? "bhmd";
            var ttlMinutes = int.TryParse(section["TtlMinutes"], out var m) ? m : 10;
            if (ttlMinutes < 1) ttlMinutes = 10;
            _ttl = TimeSpan.FromMinutes(ttlMinutes);
            _logger = logger;
        }

        public async Task<T> GetOrFetchAsync<T>(string sessionKey, int accountInfoId, string relativePath, Func<Task<T>> acquire)
        {
            if (string.IsNullOrWhiteSpace(sessionKey))
            {
                return await acquire();
            }

            using var activity = ActivitySource.StartActivity("bh.masterdata.cache");
            activity?.SetTag("bh.account_id", accountInfoId);
            activity?.SetTag("bh.path", relativePath);

            var sw = Stopwatch.StartNew();
            var db = _redis.GetDatabase();
            var pathHash = HashPath(relativePath);
            var redisKey = $"{_keyPrefix}:{sessionKey}:{accountInfoId}:{pathHash}";

            var cached = await db.StringGetAsync(redisKey);
            if (cached.HasValue)
            {
                sw.Stop();
                activity?.SetTag("bh.cache", "hit");
                activity?.SetTag("bh.redis_ms", sw.ElapsedMilliseconds);
                _logger?.LogTrace("BH master data cache hit account={AccountId} path={Path} redis_ms={RedisMs}", accountInfoId, relativePath, sw.ElapsedMilliseconds);
                if (cached == NullMarker)
                {
                    return default!;
                }

                return JsonConvert.DeserializeObject<T>(cached!, SerializerSettings)!;
            }

            sw.Restart();
            var data = await acquire();
            sw.Stop();
            activity?.SetTag("bh.cache", "miss");
            activity?.SetTag("bh.upstream_ms", sw.ElapsedMilliseconds);
            _logger?.LogTrace("BH master data cache miss account={AccountId} path={Path} upstream_ms={UpstreamMs}", accountInfoId, relativePath, sw.ElapsedMilliseconds);
            if (data == null)
            {
                await db.StringSetAsync(redisKey, NullMarker, _ttl);
                return default!;
            }

            var json = JsonConvert.SerializeObject(data, SerializerSettings);
            await db.StringSetAsync(redisKey, json, _ttl);
            return data;
        }

        private static string HashPath(string relativePath)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(relativePath));
            return Convert.ToHexString(bytes.AsSpan(0, 16));
        }
    }
}
