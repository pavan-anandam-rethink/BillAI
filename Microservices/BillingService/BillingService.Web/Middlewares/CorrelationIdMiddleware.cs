using BillingService.Application.Abstractions.Correlation;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BillingService.Web.Middlewares
{
    /// <summary>
    /// Ensures every request carries a Correlation ID (X-Correlation-Id header).
    /// Creates a new GUID when the header is absent and stores it in HttpContext.Items.
    /// The response always echoes the correlation ID back to the caller.
    /// </summary>
    public sealed class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var values)
                && !string.IsNullOrWhiteSpace(values)
                ? values.ToString()
                : Guid.NewGuid().ToString("N");

            context.Items[HeaderName] = correlationId;
            context.Response.Headers[HeaderName] = correlationId;

            await _next(context);
        }
    }
}
