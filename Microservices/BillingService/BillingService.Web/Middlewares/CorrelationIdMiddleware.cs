using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BillingService.Web.Middlewares
{
    /// <summary>
    /// Propagates or generates a <c>X-Correlation-Id</c> header for every request.
    /// If the caller provides the header it is preserved; otherwise a new GUID is assigned.
    /// The correlation ID is:
    /// <list type="bullet">
    ///   <item>Echoed on the response so clients can correlate async follow-ups.</item>
    ///   <item>Added to the current <see cref="Activity"/> baggage for distributed tracing.</item>
    ///   <item>Included in every structured log entry via a scoped log property.</item>
    /// </list>
    /// </summary>
    public sealed class CorrelationIdMiddleware
    {
        private const string HeaderName = "X-Correlation-Id";
        private const string BaggageKey = "billing.correlationId";

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(
            RequestDelegate next,
            ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = ResolveCorrelationId(context);

            // Echo on response so clients can match requests to async side-effects.
            context.Response.Headers.TryAdd(HeaderName, correlationId);

            // Make available to downstream handlers via HttpContext.Items.
            context.Items[HeaderName] = correlationId;

            // Attach to the current distributed trace activity for OpenTelemetry propagation.
            var activity = Activity.Current;
            if (activity is not null)
            {
                activity.SetBaggage(BaggageKey, correlationId);
                activity.SetTag(BaggageKey, correlationId);
            }

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                [BaggageKey] = correlationId
            }))
            {
                await _next(context);
            }
        }

        private static string ResolveCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var values))
            {
                var incoming = values.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(incoming))
                {
                    return incoming;
                }
            }

            return Guid.NewGuid().ToString("N");
        }
    }
}
