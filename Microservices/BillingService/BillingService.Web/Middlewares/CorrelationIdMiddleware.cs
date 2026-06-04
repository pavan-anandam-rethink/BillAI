using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace BillingService.Web.Middlewares
{
    /// <summary>
    /// Reads the X-Correlation-Id request header (or generates a new GUID when absent),
    /// stores it in HttpContext.Items, and echoes it back in the response header.
    /// Must be registered BEFORE <see cref="RequestLatencyLoggingMiddleware"/> so that
    /// all subsequent middleware and handlers have access to the correlation ID.
    /// </summary>
    public sealed class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";

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
            var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString("D");
            }

            context.Items[HeaderName] = correlationId;
            context.Response.Headers[HeaderName] = correlationId;

            _logger.LogDebug("CorrelationId {CorrelationId} assigned for {Method} {Path}",
                correlationId,
                context.Request.Method,
                context.Request.Path.Value);

            await _next(context);
        }
    }
}
