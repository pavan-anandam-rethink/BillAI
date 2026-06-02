using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BillingService.Web.Middlewares
{
    /// <summary>
    /// Reads or generates a Correlation ID for each request and propagates it
    /// through the response header and the structured log scope.
    /// Header name: <c>X-Correlation-Id</c>.
    /// </summary>
    public sealed class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";
        public const string ItemsKey = "CorrelationId";

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

            context.Items[ItemsKey] = correlationId;
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            using (_logger.BeginScope(new[] { new KeyValuePair<string, object>("CorrelationId", correlationId) }))
            {
                await _next(context);
            }
        }

        private static string ResolveCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var values))
            {
                var incoming = values.ToString();
                if (!string.IsNullOrWhiteSpace(incoming))
                {
                    return incoming;
                }
            }

            return Guid.NewGuid().ToString("N");
        }
    }
}
