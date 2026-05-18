using BillingService.Web.Options;

namespace BillingService.Web.XUnit.Tests.Options;

public sealed class BillingModernizationOptionsTests
{
    [Fact]
    public void Defaults_AreCompatibilitySafe()
    {
        var options = new BillingModernizationOptions();

        Assert.Equal("BillingService", options.ServiceName);
        Assert.True(options.Correlation.Enabled);
        Assert.Equal(BillingModernizationOptions.DefaultCorrelationHeaderName, options.Correlation.HeaderName);
        Assert.False(options.RateLimiting.Enabled);
        Assert.False(options.ResponseCompression.Enabled);
        Assert.True(options.OpenTelemetry.Enabled);
        Assert.Equal(1000, options.Performance.SlowRequestThresholdMs);
    }

    [Fact]
    public void Normalize_FallsBackToSafeValuesForInvalidConfiguration()
    {
        var options = new BillingModernizationOptions
        {
            ServiceName = "",
            Correlation = new CorrelationOptions
            {
                HeaderName = ""
            },
            RateLimiting = new RateLimitingOptions
            {
                PermitLimit = 0,
                QueueLimit = -1,
                WindowSeconds = 0
            },
            Performance = new PerformanceOptions
            {
                SlowRequestThresholdMs = 0
            }
        };

        options.Normalize();

        Assert.Equal("BillingService", options.ServiceName);
        Assert.Equal(BillingModernizationOptions.DefaultCorrelationHeaderName, options.Correlation.HeaderName);
        Assert.Equal(300, options.RateLimiting.PermitLimit);
        Assert.Equal(0, options.RateLimiting.QueueLimit);
        Assert.Equal(60, options.RateLimiting.WindowSeconds);
        Assert.Equal(1000, options.Performance.SlowRequestThresholdMs);
    }
}
