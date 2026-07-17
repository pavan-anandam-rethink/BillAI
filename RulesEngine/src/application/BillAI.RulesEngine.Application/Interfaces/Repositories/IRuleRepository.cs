using BillAI.RulesEngine.Domain.Entities.Rules;
using BillAI.RulesEngine.Domain.Enums;
using BillAI.RulesEngine.Shared.Models;

namespace BillAI.RulesEngine.Application.Interfaces.Repositories;

/// <summary>
/// Persistence contract for <see cref="RuleDefinition"/> aggregates.
/// </summary>
public interface IRuleRepository
{
    /// <summary>Retrieves a rule by its identifier.</summary>
    Task<RuleDefinition?> GetByIdAsync(Guid id, string tenantId, CancellationToken ct = default);

    /// <summary>Retrieves a published rule by name.</summary>
    Task<RuleDefinition?> GetByNameAsync(string name, string tenantId, CancellationToken ct = default);

    /// <summary>Retrieves all rules for a tenant, filtered by optional status and category.</summary>
    Task<PagedResult<RuleDefinition>> GetAllAsync(
        string tenantId,
        int page,
        int pageSize,
        RuleStatus? status = null,
        string? category = null,
        string? searchTerm = null,
        CancellationToken ct = default);

    /// <summary>Returns all published, enabled rules for a tenant (used by the runtime engine).</summary>
    Task<IReadOnlyList<RuleDefinition>> GetPublishedRulesAsync(string tenantId, string? category = null, CancellationToken ct = default);

    /// <summary>Persists a new rule.</summary>
    Task AddAsync(RuleDefinition rule, CancellationToken ct = default);

    /// <summary>Updates an existing rule.</summary>
    Task UpdateAsync(RuleDefinition rule, CancellationToken ct = default);

    /// <summary>Deletes a rule permanently (admin only).</summary>
    Task DeleteAsync(Guid id, string tenantId, CancellationToken ct = default);

    /// <summary>Returns whether a rule name is already taken within the tenant.</summary>
    Task<bool> ExistsAsync(string name, string tenantId, CancellationToken ct = default);
}
