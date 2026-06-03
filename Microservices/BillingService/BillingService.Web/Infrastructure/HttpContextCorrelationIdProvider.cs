using BillingService.Application.Abstractions.Correlation;
using BillingService.Web.Middlewares;
using Microsoft.AspNetCore.Http;

namespace BillingService.Web.Infrastructure
{
    /// <summary>
    /// Resolves the correlation ID stored by <see cref="CorrelationIdMiddleware"/>
    /// from the current <see cref="HttpContext"/>.
    /// Registered as scoped so each request gets a fresh resolution.
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
            var items = _httpContextAccessor.HttpContext?.Items;
            if (items is null)
            {
                return null;
            }

            return items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var value)
                ? value as string
                : null;
        }
    }
}
