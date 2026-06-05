using BillingService.Application.Abstractions.Correlation;
using BillingService.Web.Middlewares;
using Microsoft.AspNetCore.Http;

namespace BillingService.Web.Infrastructure
{
    public sealed class HttpContextCorrelationIdProvider(IHttpContextAccessor httpContextAccessor)
        : ICorrelationIdProvider
    {
        public string? GetCorrelationId()
        {
            return httpContextAccessor.HttpContext?.Items[CorrelationIdMiddleware.HeaderName] as string;
        }
    }
}
