using BillingService.App.Contracts;

namespace BillingService.App.ContractTests;

public sealed class KnownRoutesContractTests
{
    [Fact]
    public void ClaimHeadersRoute_RemainsLegacyCompatible()
    {
        Assert.Equal("Claim/GetClaimHeaders", KnownBillingRoutes.ClaimGetClaimHeaders.Path);
        Assert.Equal(HttpMethod.Post, KnownBillingRoutes.ClaimGetClaimHeaders.Method);
    }
}

