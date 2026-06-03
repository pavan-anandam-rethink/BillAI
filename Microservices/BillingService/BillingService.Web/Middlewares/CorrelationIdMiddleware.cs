using Microsoft.AspNetCore.Http;

namespace BillingService.Web.Middlewares;

/// <summary>
/// Extracts or generates a correlation ID for every inbound request.
/// The value is stored in <see cref="HttpContext.Items"/> and echoed back in the response header.
/// Downstream components resolve the correlation ID via <see cref="HttpContextCorrelationIdProvider"/>.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
            && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        await next(context).ConfigureAwait(false);
    }
}
