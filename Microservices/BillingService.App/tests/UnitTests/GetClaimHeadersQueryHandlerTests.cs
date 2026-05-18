using BillingService.App.Application.Abstractions;
using BillingService.App.Application.Claims.Queries;
using Microsoft.Extensions.Logging.Abstractions;

namespace BillingService.App.UnitTests;

public sealed class GetClaimHeadersQueryHandlerTests
{
    [Fact]
    public async Task UsesCacheWhenEntryExists()
    {
        var cache = new FakeCacheStore();
        var gateway = new FakeLegacyGateway();
        var query = new GetClaimHeadersQuery("{\"accountInfoId\":1}", new Dictionary<string, string>(), true);
        var handler = new GetClaimHeadersQueryHandler(gateway, cache, NullLogger<GetClaimHeadersQueryHandler>.Instance);

        await handler.Handle(query, CancellationToken.None);
        await handler.Handle(query, CancellationToken.None);

        Assert.Equal(1, gateway.CallCount);
    }

    private sealed class FakeLegacyGateway : ILegacyBillingGateway
    {
        public int CallCount { get; private set; }

        public Task<LegacyGatewayResponse> ForwardJsonAsync(
            string relativePath,
            string jsonPayload,
            IReadOnlyDictionary<string, string> headers,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new LegacyGatewayResponse(200, "{\"ok\":true}", "application/json"));
        }
    }

    private sealed class FakeCacheStore : ICacheStore
    {
        private readonly Dictionary<string, string> _cache = new();

        public Task<string?> GetStringAsync(string key, CancellationToken cancellationToken)
        {
            _cache.TryGetValue(key, out var value);
            return Task.FromResult(value);
        }

        public Task SetStringAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken)
        {
            _cache[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

