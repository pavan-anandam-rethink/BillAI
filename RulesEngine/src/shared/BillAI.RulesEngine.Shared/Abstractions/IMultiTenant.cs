namespace BillAI.RulesEngine.Shared.Abstractions;

/// <summary>
/// Represents a multi-tenant entity that is scoped to a specific tenant.
/// </summary>
public interface IMultiTenant
{
    string TenantId { get; }
}
