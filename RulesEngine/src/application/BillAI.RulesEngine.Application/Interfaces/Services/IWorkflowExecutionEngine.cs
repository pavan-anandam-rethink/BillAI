using BillAI.RulesEngine.Domain.Entities.Workflows;

namespace BillAI.RulesEngine.Application.Interfaces.Services;

/// <summary>
/// Contract for the workflow runtime engine that executes workflow instances.
/// </summary>
public interface IWorkflowExecutionEngine
{
    /// <summary>Creates and starts a new workflow instance.</summary>
    Task<WorkflowInstance> StartAsync(
        WorkflowDefinition definition,
        string tenantId,
        string startedBy,
        string? inputDataJson = null,
        string? correlationId = null,
        CancellationToken ct = default);

    /// <summary>Resumes a suspended workflow instance.</summary>
    Task<WorkflowInstance> ResumeAsync(Guid instanceId, string tenantId, string? resumeDataJson = null, CancellationToken ct = default);

    /// <summary>Cancels a running or suspended instance.</summary>
    Task<WorkflowInstance> CancelAsync(Guid instanceId, string tenantId, CancellationToken ct = default);

    /// <summary>Retries a failed instance.</summary>
    Task<WorkflowInstance> RetryAsync(Guid instanceId, string tenantId, CancellationToken ct = default);
}
