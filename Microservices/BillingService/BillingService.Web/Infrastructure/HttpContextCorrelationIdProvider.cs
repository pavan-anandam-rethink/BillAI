using BillingService.Application.Abstractions.Correlation;
using BillingService.Web.Middlewares;
using Microsoft.AspNetCore.Http;

namespace BillingService.Web.Infrastructure
{
    /// <summary>
    /// Reads the correlation ID stored by <see cref="CorrelationIdMiddleware"/> from
    /// the ambient <see cref="IHttpContextAccessor"/>. Falls back to a new GUID when
    /// no HTTP context is present (e.g. background jobs or outbox workers).
    /// </summary>
    internal sealed class HttpContextCorrelationIdProvider(IHttpContextAccessor httpContextAccessor)
        : ICorrelationIdProvider
    {
        public string GetCorrelationId()
        {
            var value = httpContextAccessor.HttpContext?.Items[CorrelationIdMiddleware.HeaderName];
            if (value is string id && !string.IsNullOrWhiteSpace(id))
            {
                return id;
            }

            return Guid.NewGuid().ToString("N");
        }
    }
}
