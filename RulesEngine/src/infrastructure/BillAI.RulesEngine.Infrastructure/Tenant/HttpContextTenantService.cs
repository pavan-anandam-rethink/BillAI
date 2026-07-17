using BillAI.RulesEngine.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace BillAI.RulesEngine.Infrastructure.Tenant;

/// <summary>
/// Resolves the current tenant ID from the HTTP request context.
/// Reads from the X-Tenant-Id header, falling back to route/query or a default.
/// </summary>
public sealed class HttpContextTenantService(IHttpContextAccessor httpContextAccessor) : ITenantService
{
    private const string TenantHeader = "X-Tenant-Id";
    private const string DefaultTenant = "default";

    /// <inheritdoc/>
    public string GetCurrentTenantId()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null) return DefaultTenant;

        if (context.Request.Headers.TryGetValue(TenantHeader, out var tenantHeader)
            && !string.IsNullOrWhiteSpace(tenantHeader))
            return tenantHeader.ToString();

        // Check query string
        if (context.Request.QueryString.HasValue)
        {
            var qs = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(context.Request.QueryString.Value ?? string.Empty);
            if (qs.TryGetValue("tenantId", out var qsTenant) && !string.IsNullOrWhiteSpace(qsTenant))
                return qsTenant.ToString();
        }

        return DefaultTenant;
    }

    /// <inheritdoc/>
    public Task<bool> TenantExistsAsync(string tenantId, CancellationToken ct = default)
        => Task.FromResult(!string.IsNullOrWhiteSpace(tenantId));
}
