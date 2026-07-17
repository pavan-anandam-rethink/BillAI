using BillAI.RulesEngine.Domain.Common;

namespace BillAI.RulesEngine.Domain.Events.Workflows;

/// <summary>Raised when a new workflow definition is created.</summary>
public sealed class WorkflowCreatedEvent(Guid workflowId, string tenantId, string workflowName, string createdBy)
    : DomainEvent
{
    public Guid WorkflowId { get; } = workflowId;
    public string TenantId { get; } = tenantId;
    public string WorkflowName { get; } = workflowName;
    public string CreatedBy { get; } = createdBy;
}

/// <summary>Raised when a workflow is published.</summary>
public sealed class WorkflowPublishedEvent(Guid workflowId, string tenantId, string workflowName, int version, string publishedBy)
    : DomainEvent
{
    public Guid WorkflowId { get; } = workflowId;
    public string TenantId { get; } = tenantId;
    public string WorkflowName { get; } = workflowName;
    public int Version { get; } = version;
    public string PublishedBy { get; } = publishedBy;
}

/// <summary>Raised when a workflow instance completes execution.</summary>
public sealed class WorkflowInstanceCompletedEvent(Guid instanceId, string tenantId, bool succeeded)
    : DomainEvent
{
    public Guid InstanceId { get; } = instanceId;
    public string TenantId { get; } = tenantId;
    public bool Succeeded { get; } = succeeded;
}
