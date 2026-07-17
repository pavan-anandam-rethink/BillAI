namespace BillAI.RulesEngine.Application.Interfaces.Services;

/// <summary>
/// Contract for managing tenant configurations.
/// </summary>
public interface ITenantService
{
    /// <summary>Returns the current tenant identifier from the request context.</summary>
    string GetCurrentTenantId();

    /// <summary>Returns true if the specified tenant exists and is active.</summary>
    Task<bool> TenantExistsAsync(string tenantId, CancellationToken ct = default);
}
