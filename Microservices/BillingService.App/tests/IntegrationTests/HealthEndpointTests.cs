namespace BillingService.App.IntegrationTests;

public sealed class HealthEndpointTests
{
    [Fact(Skip = "Requires API host bootstrap in CI runtime.")]
    public Task HealthCheckEndpoint_IsReachable()
    {
        return Task.CompletedTask;
    }
}

