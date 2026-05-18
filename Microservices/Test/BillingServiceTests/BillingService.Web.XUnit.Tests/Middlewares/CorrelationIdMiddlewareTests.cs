using BillingService.Web.Middlewares;
using BillingService.Web.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BillingService.Web.XUnit.Tests.Middlewares;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_PreservesExistingCorrelationId()
    {
        var options = Options.Create(new BillingModernizationOptions());
        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            options,
            NullLogger<CorrelationIdMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Headers[BillingModernizationOptions.DefaultCorrelationHeaderName] = "existing-correlation-id";

        await middleware.InvokeAsync(context);

        Assert.Equal("existing-correlation-id", context.TraceIdentifier);
        Assert.Equal("existing-correlation-id", context.Response.Headers[BillingModernizationOptions.DefaultCorrelationHeaderName].ToString());
    }

    [Fact]
    public async Task InvokeAsync_GeneratesCorrelationIdWhenMissing()
    {
        var options = Options.Create(new BillingModernizationOptions());
        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            options,
            NullLogger<CorrelationIdMiddleware>.Instance);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        var responseCorrelationId = context.Response.Headers[BillingModernizationOptions.DefaultCorrelationHeaderName].ToString();
        Assert.False(string.IsNullOrWhiteSpace(responseCorrelationId));
        Assert.Equal(context.TraceIdentifier, responseCorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_LeavesResponseHeaderUnsetWhenDisabled()
    {
        var options = Options.Create(new BillingModernizationOptions
        {
            Correlation = new CorrelationOptions
            {
                Enabled = false
            }
        });
        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            options,
            NullLogger<CorrelationIdMiddleware>.Instance);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.False(context.Response.Headers.ContainsKey(BillingModernizationOptions.DefaultCorrelationHeaderName));
    }
}
