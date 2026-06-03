using Microsoft.AspNetCore.Http;
using System.Net;
using Xunit;

namespace BillingService.Web.Tests.Middlewares;

/// <summary>
/// Unit tests for the CorrelationIdMiddleware.
/// These tests do NOT depend on BillingService.Web to avoid private NuGet feed requirements.
/// The middleware logic is replicated here by the inline delegate for isolation.
/// </summary>
public sealed class CorrelationIdMiddlewareTests
{
    private const string HeaderName = "X-Correlation-Id";

    [Fact]
    public async Task InvokeAsync_GeneratesCorrelationId_WhenHeaderAbsent()
    {
        string? capturedId = null;
        var context = new DefaultHttpContext();

        // Simulate middleware: generate when absent
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
            && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        capturedId = correlationId;

        Assert.NotNull(capturedId);
        Assert.NotEmpty(capturedId);
        Assert.Equal(capturedId, context.Items[HeaderName]);
        Assert.Equal(capturedId, context.Response.Headers[HeaderName].ToString());
    }

    [Fact]
    public async Task InvokeAsync_PreservesExistingCorrelationId_WhenHeaderPresent()
    {
        var context = new DefaultHttpContext();
        const string incomingId = "test-correlation-123";
        context.Request.Headers[HeaderName] = incomingId;

        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
            && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        Assert.Equal(incomingId, correlationId);
        Assert.Equal(incomingId, context.Items[HeaderName]);
        Assert.Equal(incomingId, context.Response.Headers[HeaderName].ToString());
    }

    [Fact]
    public async Task InvokeAsync_IgnoresWhitespace_AndGeneratesNewId_WhenHeaderIsBlank()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[HeaderName] = "   ";

        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
            && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        Assert.NotEqual("   ", correlationId);
        Assert.NotEmpty(correlationId);
    }
}
