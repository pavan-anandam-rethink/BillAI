using BillAI.RulesEngine.Domain.Entities.Workflows;
using BillAI.RulesEngine.Domain.Enums;
using BillAI.RulesEngine.Shared.Models;

namespace BillAI.RulesEngine.Application.Interfaces.Repositories;

/// <summary>
/// Persistence contract for <see cref="WorkflowDefinition"/> aggregates.
/// </summary>
public interface IWorkflowRepository
{
    Task<WorkflowDefinition?> GetByIdAsync(Guid id, string tenantId, CancellationToken ct = default);
    Task<WorkflowDefinition?> GetByNameAsync(string name, string tenantId, CancellationToken ct = default);
    Task<PagedResult<WorkflowDefinition>> GetAllAsync(
        string tenantId,
        int page,
        int pageSize,
        WorkflowStatus? status = null,
        CancellationToken ct = default);
    Task<IReadOnlyList<WorkflowDefinition>> GetPublishedWorkflowsAsync(string tenantId, CancellationToken ct = default);
    Task AddAsync(WorkflowDefinition workflow, CancellationToken ct = default);
    Task UpdateAsync(WorkflowDefinition workflow, CancellationToken ct = default);
    Task DeleteAsync(Guid id, string tenantId, CancellationToken ct = default);
}
