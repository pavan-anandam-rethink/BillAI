using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BillingService.Web.Observability
{
    /// <summary>
    /// Captures request latency and active request concurrency with low allocation overhead.
    /// </summary>
    public sealed class RequestTelemetryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTelemetryMiddleware> _logger;

        public RequestTelemetryMiddleware(
            RequestDelegate next,
            ILogger<RequestTelemetryMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var endpoint = context.GetEndpoint()?.DisplayName
                ?? context.Request.Path.Value
                ?? string.Empty;

            ConcurrencyMetrics.RequestStarted(context.Request.Method, endpoint);
            var stopwatch = Stopwatch.StartNew();
            var hasException = false;

            try
            {
                await _next(context).ConfigureAwait(false);
            }
            catch
            {
                hasException = true;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                ConcurrencyMetrics.RequestCompleted(
                    context.Request.Method,
                    endpoint,
                    context.Response.StatusCode,
                    stopwatch.Elapsed.TotalMilliseconds,
                    hasException);

                _logger.LogInformation(
                    "RequestTelemetry method={Method} endpoint={Endpoint} statusCode={StatusCode} latencyMs={LatencyMs}",
                    context.Request.Method,
                    endpoint,
                    context.Response.StatusCode,
                    stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }
}
