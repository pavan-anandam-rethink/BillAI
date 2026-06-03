using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace BillingService.Web.Middlewares
{
    /// <summary>
    /// Ensures every request carries a correlation ID.
    /// If the <c>X-Correlation-Id</c> header is present, its value is preserved.
    /// Otherwise a new GUID is generated and added to the request/response headers.
    /// The value is stored in <see cref="HttpContext.Items"/> so that
    /// <c>HttpContextCorrelationIdProvider</c> can expose it to Application layer services.
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
            var correlationId = GetOrCreate(context);

            context.Items[HeaderName] = correlationId;
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

        private static string GetOrCreate(HttpContext context)
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
