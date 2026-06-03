using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace BillingService.Web.Middlewares
{
    /// <summary>
    /// Reads or generates an X-Correlation-Id header for every incoming request and
    /// stores the value in <see cref="HttpContext.Items"/> so that downstream middleware,
    /// controllers, and services can read it via <see cref="ICorrelationIdProvider"/>.
    /// The correlation ID is also echoed back on the response.
    /// </summary>
    public sealed class CorrelationIdMiddleware
    {
        /// <summary>
        /// The HTTP header name and HttpContext.Items key used for the correlation ID.
        /// </summary>
        public const string HeaderName = "X-Correlation-Id";

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = ResolveCorrelationId(context);
            context.Items[HeaderName] = correlationId;

            // Echo the correlation ID back in the response so callers can trace the round-trip.
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(HeaderName))
                {
                    context.Response.Headers[HeaderName] = correlationId;
                }

                return Task.CompletedTask;
            });

            await _next(context);
        }

        private static string ResolveCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out StringValues values)
                && !StringValues.IsNullOrEmpty(values))
            {
                var value = values.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            // Generate a new correlation ID when the caller did not provide one.
            return Guid.NewGuid().ToString("N");
        }
    }
}
