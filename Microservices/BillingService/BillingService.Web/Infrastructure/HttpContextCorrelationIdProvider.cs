using BillingService.Application.Abstractions.Correlation;
using BillingService.Web.Middlewares;
using Microsoft.AspNetCore.Http;

namespace BillingService.Web.Infrastructure
{
    /// <summary>
    /// Resolves the current correlation ID from the HTTP context.
    /// Populated by <see cref="CorrelationIdMiddleware"/> which must be registered
    /// in the pipeline before this provider is first used.
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
