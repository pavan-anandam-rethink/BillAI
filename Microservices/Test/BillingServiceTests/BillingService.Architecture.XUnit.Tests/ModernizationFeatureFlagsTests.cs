using BillingService.Application.Common.Configuration;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class ModernizationFeatureFlagsTests
{
    [Fact]
    public void DefaultFlags_AreAllFalse()
    {
        var flags = new ModernizationFeatureFlags();

        Assert.False(flags.EnableCleanArchitectureAdapters);
        Assert.False(flags.EnableDistributedCacheDecorators);
        Assert.False(flags.EnableOutboxPublisher);
        Assert.False(flags.EnableReadModelQueries);
        Assert.False(flags.EnableBlobFirstStorage);
    }

    [Fact]
    public void SectionName_IsCorrect()
    {
        Assert.Equal("BillingService:Modernization", ModernizationFeatureFlags.SectionName);
    }
}
