using BillingService.Application.Abstractions.Correlation;
using BillingService.Web.Middlewares;
using Microsoft.AspNetCore.Http;

namespace BillingService.Web.Infrastructure
{
    /// <summary>
    /// Resolves the correlation ID from the current <see cref="HttpContext"/>.
    /// The value was populated by <see cref="CorrelationIdMiddleware"/> before this is called.
    /// Returns null when invoked outside an active HTTP request (e.g. from a background worker).
    /// </summary>
    internal sealed class HttpContextCorrelationIdProvider : ICorrelationIdProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextCorrelationIdProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? GetCorrelationId()
        {
            return _httpContextAccessor.HttpContext?.Items[CorrelationIdMiddleware.HeaderName] as string;
        }
    }
}
