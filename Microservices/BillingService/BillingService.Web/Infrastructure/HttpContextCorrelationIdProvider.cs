using BillingService.Application.Abstractions.Correlation;
using BillingService.Web.Middlewares;
using Microsoft.AspNetCore.Http;

namespace BillingService.Web.Infrastructure
{
    /// <summary>
    /// Resolves the correlation ID from the current <see cref="HttpContext"/> populated
    /// by <see cref="CorrelationIdMiddleware"/>. Registered as a scoped service so each
    /// request gets an isolated view of the correlation ID.
    /// </summary>
    public sealed class HttpContextCorrelationIdProvider : ICorrelationIdProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextCorrelationIdProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? GetCorrelationId()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null)
            {
                return null;
            }

            if (context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var value)
                && value is string correlationId
                && !string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId;
            }

            // Fallback: read directly from the request header if middleware stamp is absent.
            if (context.Request.Headers.TryGetValue(CorrelationIdMiddleware.HeaderName, out var header)
                && !string.IsNullOrWhiteSpace(header))
            {
                return header.ToString();
            }

            return null;
        }
    }
}
