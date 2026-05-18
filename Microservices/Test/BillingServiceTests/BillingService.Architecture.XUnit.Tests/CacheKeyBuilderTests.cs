using BillingService.Application.Abstractions.Caching;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class CacheKeyBuilderTests
{
    private readonly CacheKeyBuilder _builder = new();

    [Fact]
    public void BuildTenantKey_UsesStableRedisClusterHashTag()
    {
        var key = _builder.BuildTenantKey(42, "Dashboard Summary", "Page 1", "Sort:Desc");

        Assert.Equal("billing:{tenant:42}:dashboard-summary:page-1:sort-desc", key);
    }

    [Fact]
    public void BuildGlobalKey_UsesSharedPartition()
    {
        var key = _builder.BuildGlobalKey("Lookup Data", "States");

        Assert.Equal("billing:{global:shared}:lookup-data:states", key);
    }

    [Fact]
    public void BuildTenantKey_RejectsInvalidTenantIds()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _builder.BuildTenantKey(0, "dashboard"));
    }
}
