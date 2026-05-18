using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using BillingService.Web.Telemetry;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BillingService.Web.Middlewares
{
    /// <summary>
    /// Logs per-request duration for API traffic (excludes swagger and health).
    /// </summary>
    public sealed class RequestLatencyLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLatencyLoggingMiddleware> _logger;

        public RequestLatencyLoggingMiddleware(RequestDelegate next, ILogger<RequestLatencyLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if (ShouldSkip(path))
            {
                await _next(context);
                return;
            }

            var sw = Stopwatch.StartNew();
            using var activity = BillingTelemetry.ActivitySource.StartActivity($"{context.Request.Method} {path}", ActivityKind.Server);
            activity?.SetTag("http.request.method", context.Request.Method);
            activity?.SetTag("url.path", path);

            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                activity?.SetTag("http.response.status_code", context.Response.StatusCode);
                BillingTelemetry.HttpServerRequests.Add(1);
                BillingTelemetry.HttpServerDurationMs.Record(sw.Elapsed.TotalMilliseconds);
                _logger.LogInformation(
                    "HTTP {Method} {Path} completed in {ElapsedMs} ms with status {StatusCode}",
                    context.Request.Method,
                    path,
                    sw.ElapsedMilliseconds,
                    context.Response.StatusCode);
            }
        }

        private static bool ShouldSkip(string path)
        {
            return path.StartsWith("/swagger", System.StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/api/health", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
