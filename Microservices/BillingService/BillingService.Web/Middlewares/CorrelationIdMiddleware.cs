using BillingService.Application.Abstractions.Correlation;
using Microsoft.AspNetCore.Http;

namespace BillingService.Web.Middlewares
{
    /// <summary>
    /// Reads (or generates) the X-Correlation-Id request header and stamps it on the
    /// response. Stores the value in <see cref="HttpContext.Items"/> for downstream
    /// resolution by <see cref="Infrastructure.HttpContextCorrelationIdProvider"/>.
    /// Must be registered BEFORE <see cref="RequestLatencyLoggingMiddleware"/> so that
    /// latency logs include the correlation ID.
    /// </summary>
    public sealed class CorrelationIdMiddleware
    {
        /// <summary>The HTTP header name used to propagate the correlation ID.</summary>
        public const string HeaderName = "X-Correlation-Id";

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = GetOrCreate(context);

            context.Items[HeaderName] = correlationId;

            if (!context.Response.Headers.ContainsKey(HeaderName))
            {
                context.Response.Headers.Append(HeaderName, correlationId);
            }

            await _next(context);
        }

        private static string GetOrCreate(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var existing)
                && !string.IsNullOrWhiteSpace(existing))
            {
                return existing.ToString();
            }

            return Guid.NewGuid().ToString("D");
        }
    }
}
