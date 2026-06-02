using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BillingService.Web.Middlewares
{
    /// <summary>
    /// Reads or generates a correlation ID for every request and propagates it through
    /// the structured log scope and the response header so distributed traces can be
    /// joined end-to-end across services.
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
            var correlationId = ResolveCorrelationId(context);

            // Propagate to response so callers can correlate log entries.
            context.Response.Headers[HeaderName] = correlationId;

            // Store in HttpContext.Items so downstream middleware and controllers can read it.
            context.Items[HeaderName] = correlationId;

            // Link to the current Activity (OpenTelemetry distributed trace).
            Activity.Current?.SetTag("correlation.id", correlationId);

            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
            {
                await _next(context).ConfigureAwait(false);
            }
        }

        private static string ResolveCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var incoming)
                && !string.IsNullOrWhiteSpace(incoming))
            {
                return incoming.ToString().Trim();
            }

            // Fall back to the TraceIdentifier set by Kestrel, which maps to the
            // current W3C trace-parent when ASP.NET Core activity tracking is on.
            if (!string.IsNullOrWhiteSpace(context.TraceIdentifier))
            {
                return context.TraceIdentifier;
            }

            return Guid.NewGuid().ToString("N");
        }
    }
}
