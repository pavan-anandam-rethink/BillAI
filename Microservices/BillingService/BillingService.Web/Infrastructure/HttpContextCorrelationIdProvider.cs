using BillingService.Application.Abstractions.Correlation;
using BillingService.Web.Middlewares;
using Microsoft.AspNetCore.Http;

namespace BillingService.Web.Infrastructure;

/// <summary>
/// Resolves the correlation ID from the current HTTP context item set by <see cref="CorrelationIdMiddleware"/>.
/// </summary>
public sealed class HttpContextCorrelationIdProvider(IHttpContextAccessor httpContextAccessor) : ICorrelationIdProvider
{
    public string CorrelationId =>
        httpContextAccessor.HttpContext?.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var value) == true
        && value is string id
        ? id
        : string.Empty;
}
