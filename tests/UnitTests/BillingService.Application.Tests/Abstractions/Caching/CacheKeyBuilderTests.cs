using BillingService.Application.Abstractions.Caching;
using Xunit;

namespace BillingService.Application.Tests.Abstractions.Caching;

public sealed class CacheKeyBuilderTests
{
    private readonly CacheKeyBuilder _builder = new();

    [Fact]
    public void BuildTenantKey_ProducesExpectedPrefix()
    {
        var key = _builder.BuildTenantKey(42, "dashboard");

        Assert.StartsWith("billing:{tenant:42}:dashboard:", key);
    }

    [Fact]
    public void BuildGlobalKey_ProducesExpectedPrefix()
    {
        var key = _builder.BuildGlobalKey("lookups");

        Assert.StartsWith("billing:{global:shared}:lookups:", key);
    }

    [Fact]
    public void BuildTenantKey_NormalizesArea()
    {
        var key1 = _builder.BuildTenantKey(1, "Dashboard Summary");
        var key2 = _builder.BuildTenantKey(1, "dashboard-summary");

        Assert.Equal(key1, key2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BuildTenantKey_ThrowsArgumentOutOfRangeException_WhenAccountIdIsNotPositive(int accountId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _builder.BuildTenantKey(accountId, "area"));
    }

    [Fact]
    public void BuildTenantKey_ThrowsArgumentException_WhenAreaIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => _builder.BuildTenantKey(1, ""));
    }

    [Fact]
    public void BuildTenantKey_WithParts_ProducesStableKey()
    {
        var key1 = _builder.BuildTenantKey(5, "claims", 123, "open");
        var key2 = _builder.BuildTenantKey(5, "claims", 123, "open");

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void BuildTenantKey_WithDifferentParts_ProducesDifferentKeys()
    {
        var key1 = _builder.BuildTenantKey(5, "claims", 123);
        var key2 = _builder.BuildTenantKey(5, "claims", 456);

        Assert.NotEqual(key1, key2);
    }
}
