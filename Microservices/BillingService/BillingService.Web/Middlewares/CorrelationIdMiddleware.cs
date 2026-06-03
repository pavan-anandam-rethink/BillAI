using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BillingService.Web.Middlewares
{
    /// <summary>
    /// Reads or generates the X-Correlation-Id header and stores it in HttpContext.Items
    /// so that ICorrelationIdProvider and structured logs can reference it throughout the request.
    /// Registered before RequestLatencyLoggingMiddleware in the pipeline.
    /// </summary>
    public sealed class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = GetOrCreateCorrelationId(context);

            context.Items[HeaderName] = correlationId;
            context.Response.Headers[HeaderName] = correlationId;

            using (_logger.BeginScope("{CorrelationId}", correlationId))
            {
                await _next(context);
            }
        }

        private static string GetOrCreateCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var existing)
                && !string.IsNullOrWhiteSpace(existing))
            {
                return existing.ToString();
            }

            return Guid.NewGuid().ToString("N");
        }
    }
}
