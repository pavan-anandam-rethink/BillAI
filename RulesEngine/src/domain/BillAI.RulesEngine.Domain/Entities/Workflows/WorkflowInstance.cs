using BillAI.RulesEngine.Domain.Common;
using BillAI.RulesEngine.Domain.Enums;
using BillAI.RulesEngine.Domain.Events.Workflows;

namespace BillAI.RulesEngine.Domain.Entities.Workflows;

/// <summary>
/// Represents a running instance of a workflow, tracking execution state.
/// </summary>
public sealed class WorkflowInstance : AuditableEntity
{
    private readonly List<DomainEvent> _domainEvents = [];
    private readonly List<WorkflowExecutionStep> _steps = [];

    private WorkflowInstance() { }

    public Guid WorkflowId { get; private set; }
    public string WorkflowName { get; private set; } = string.Empty;
    public int WorkflowVersion { get; private set; }
    public string? CorrelationId { get; private set; }
    public WorkflowInstanceStatus Status { get; private set; } = WorkflowInstanceStatus.Pending;
    public string? CurrentNodeId { get; private set; }
    public string? InputDataJson { get; private set; }
    public string? OutputDataJson { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int RetryCount { get; private set; }

    /// <summary>Gets the serialised workflow variables dictionary (JSON).</summary>
    public string? VariablesJson { get; private set; }

    public IReadOnlyList<WorkflowExecutionStep> Steps => _steps.AsReadOnly();

    /// <summary>Pops and clears all pending domain events.</summary>
    public IReadOnlyList<DomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }

    /// <summary>Factory method to create a new pending workflow instance.</summary>
    public static WorkflowInstance Create(
        string tenantId,
        Guid workflowId,
        string workflowName,
        int workflowVersion,
        string createdBy,
        string? inputDataJson = null,
        string? correlationId = null)
        => new()
        {
            TenantId = tenantId,
            WorkflowId = workflowId,
            WorkflowName = workflowName,
            WorkflowVersion = workflowVersion,
            InputDataJson = inputDataJson,
            CorrelationId = correlationId,
            CreatedBy = createdBy
        };

    /// <summary>Transitions the instance to the Running state.</summary>
    public void Start()
    {
        Status = WorkflowInstanceStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Records a completed execution step.</summary>
    public void RecordStep(string nodeId, string nodeName, bool succeeded, string? output = null, string? error = null)
    {
        var step = WorkflowExecutionStep.Create(Id, nodeId, nodeName, succeeded, output, error);
        _steps.Add(step);
        CurrentNodeId = nodeId;
    }

    /// <summary>Marks the instance as successfully completed.</summary>
    public void Complete(string? outputDataJson = null)
    {
        Status = WorkflowInstanceStatus.Succeeded;
        OutputDataJson = outputDataJson;
        CompletedAt = DateTimeOffset.UtcNow;
        _domainEvents.Add(new WorkflowInstanceCompletedEvent(Id, TenantId, true));
    }

    /// <summary>Marks the instance as failed.</summary>
    public void Fail(string errorMessage)
    {
        Status = WorkflowInstanceStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTimeOffset.UtcNow;
        _domainEvents.Add(new WorkflowInstanceCompletedEvent(Id, TenantId, false));
    }

    /// <summary>Suspends the instance for external input.</summary>
    public void Suspend()
        => Status = WorkflowInstanceStatus.Suspended;

    /// <summary>Resumes a suspended instance.</summary>
    public void Resume()
        => Status = WorkflowInstanceStatus.Running;

    /// <summary>Cancels the instance.</summary>
    public void Cancel()
    {
        Status = WorkflowInstanceStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Marks the instance as timed out.</summary>
    public void Timeout()
    {
        Status = WorkflowInstanceStatus.TimedOut;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Increments retry counter and resets to Running.</summary>
    public void Retry()
    {
        RetryCount++;
        Status = WorkflowInstanceStatus.Running;
    }

    /// <summary>Updates the serialised variables dictionary.</summary>
    public void SetVariables(string variablesJson)
        => VariablesJson = variablesJson;
}
