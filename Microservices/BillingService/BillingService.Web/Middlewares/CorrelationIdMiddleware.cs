using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BillingService.Web.Middlewares
{
    /// <summary>
    /// Reads an incoming X-Correlation-Id header (or generates one) and:
    /// 1. Echoes it on the response.
    /// 2. Stamps it onto the current Activity so OpenTelemetry trace spans carry it.
    /// 3. Stores it in HttpContext.Items for downstream use within the request lifetime.
    /// </summary>
    public sealed class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";
        public const string HttpContextItemKey = "CorrelationId";

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

            context.Items[HttpContextItemKey] = correlationId;
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(HeaderName))
                {
                    context.Response.Headers[HeaderName] = correlationId;
                }
                return Task.CompletedTask;
            });

            var activity = Activity.Current;
            if (activity is not null)
            {
                activity.AddTag("correlation.id", correlationId);
                activity.SetBaggage("correlation.id", correlationId);
            }

            using (_logger.BeginScope(new System.Collections.Generic.Dictionary<string, object>
            {
                [HttpContextItemKey] = correlationId
            }))
            {
                await _next(context);
            }
        }

        private static string ResolveCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var incoming)
                && !string.IsNullOrWhiteSpace(incoming))
            {
                return incoming.ToString();
            }

            return Guid.NewGuid().ToString("D");
        }
    }
}
