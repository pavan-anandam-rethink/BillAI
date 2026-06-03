using BillingService.Application.Abstractions.Caching;
using Xunit;

namespace BillingService.Application.Tests.Abstractions.Caching;

public sealed class BillingCacheEntryOptionsTests
{
    [Fact]
    public void DashboardSummary_HasTwoMinuteTtl_AndIsTenantScoped()
    {
        Assert.Equal(TimeSpan.FromMinutes(2), BillingCacheEntryOptions.DashboardSummary.Ttl);
        Assert.True(BillingCacheEntryOptions.DashboardSummary.TenantScoped);
    }

    [Fact]
    public void LookupData_HasSixHourTtl_AndIsNotTenantScoped()
    {
        Assert.Equal(TimeSpan.FromHours(6), BillingCacheEntryOptions.LookupData.Ttl);
        Assert.False(BillingCacheEntryOptions.LookupData.TenantScoped);
    }

    [Fact]
    public void Constructor_ThrowsArgumentOutOfRangeException_WhenTtlIsZeroOrNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BillingCacheEntryOptions(TimeSpan.Zero, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BillingCacheEntryOptions(TimeSpan.FromSeconds(-1), false));
    }

    [Fact]
    public void Constructor_AssignsTtlAndTenantScoped_WhenArgumentsAreValid()
    {
        var options = new BillingCacheEntryOptions(TimeSpan.FromMinutes(7), tenantScoped: true);

        Assert.Equal(TimeSpan.FromMinutes(7), options.Ttl);
        Assert.True(options.TenantScoped);
    }
}
