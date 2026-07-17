using BillAI.RulesEngine.Domain.Entities.Workflows;
using BillAI.RulesEngine.Domain.Enums;
using BillAI.RulesEngine.Shared.Models;

namespace BillAI.RulesEngine.Application.Interfaces.Repositories;

/// <summary>
/// Persistence contract for <see cref="WorkflowInstance"/> aggregates.
/// </summary>
public interface IWorkflowInstanceRepository
{
    Task<WorkflowInstance?> GetByIdAsync(Guid id, string tenantId, CancellationToken ct = default);
    Task<PagedResult<WorkflowInstance>> GetAllAsync(
        string tenantId,
        int page,
        int pageSize,
        WorkflowInstanceStatus? status = null,
        Guid? workflowId = null,
        CancellationToken ct = default);
    Task AddAsync(WorkflowInstance instance, CancellationToken ct = default);
    Task UpdateAsync(WorkflowInstance instance, CancellationToken ct = default);
}
