using BillAI.RulesEngine.Domain.Common;
using BillAI.RulesEngine.Domain.Enums;
using BillAI.RulesEngine.Domain.Events.Workflows;

namespace BillAI.RulesEngine.Domain.Entities.Workflows;

/// <summary>
/// Represents a workflow definition with nodes, transitions, and versioning.
/// </summary>
public sealed class WorkflowDefinition : AuditableEntity
{
    private readonly List<DomainEvent> _domainEvents = [];
    private readonly List<WorkflowNode> _nodes = [];
    private readonly List<WorkflowTransition> _transitions = [];
    private readonly List<WorkflowVersion> _versions = [];

    private WorkflowDefinition() { }

    /// <summary>Gets the unique workflow name within the tenant.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the description of the workflow.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the category/domain for this workflow.</summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>Gets the current lifecycle status.</summary>
    public WorkflowStatus Status { get; private set; } = WorkflowStatus.Draft;

    /// <summary>Gets the current version number.</summary>
    public int Version { get; private set; } = 1;

    /// <summary>Gets the workflow nodes.</summary>
    public IReadOnlyList<WorkflowNode> Nodes => _nodes.AsReadOnly();

    /// <summary>Gets the transitions between nodes.</summary>
    public IReadOnlyList<WorkflowTransition> Transitions => _transitions.AsReadOnly();

    /// <summary>Gets the version history.</summary>
    public IReadOnlyList<WorkflowVersion> Versions => _versions.AsReadOnly();

    /// <summary>Gets the serialised workflow definition JSON (layout + metadata).</summary>
    public string? DefinitionJson { get; private set; }

    /// <summary>Gets the default timeout for workflow instances in minutes (null = no timeout).</summary>
    public int? TimeoutMinutes { get; private set; }

    /// <summary>Pops and clears all pending domain events.</summary>
    public IReadOnlyList<DomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }

    /// <summary>Factory method to create a new draft workflow.</summary>
    public static WorkflowDefinition Create(
        string tenantId,
        string name,
        string category,
        string createdBy,
        string? description = null,
        int? timeoutMinutes = null)
    {
        var workflow = new WorkflowDefinition
        {
            TenantId = tenantId,
            Name = name,
            Description = description,
            Category = category,
            TimeoutMinutes = timeoutMinutes,
            CreatedBy = createdBy
        };

        workflow._domainEvents.Add(new WorkflowCreatedEvent(workflow.Id, tenantId, name, createdBy));
        return workflow;
    }

    /// <summary>Adds a node to the workflow.</summary>
    public WorkflowNode AddNode(string nodeId, WorkflowNodeType type, string label, string? configJson = null)
    {
        if (_nodes.Any(n => n.NodeId == nodeId))
            throw new InvalidOperationException($"Node '{nodeId}' already exists in the workflow.");

        var node = WorkflowNode.Create(Id, nodeId, type, label, configJson);
        _nodes.Add(node);
        return node;
    }

    /// <summary>Adds a transition between two nodes.</summary>
    public WorkflowTransition AddTransition(string sourceNodeId, string targetNodeId, string? condition = null, string? label = null)
    {
        var transition = WorkflowTransition.Create(Id, sourceNodeId, targetNodeId, condition, label);
        _transitions.Add(transition);
        return transition;
    }

    /// <summary>Publishes the workflow, making it available for execution.</summary>
    public void Publish(string publishedBy)
    {
        ValidateStructure();
        _versions.Add(WorkflowVersion.Snapshot(this));

        Status = WorkflowStatus.Published;
        Touch(publishedBy);
        _domainEvents.Add(new WorkflowPublishedEvent(Id, TenantId, Name, Version, publishedBy));
    }

    /// <summary>Archives the workflow, preventing new executions.</summary>
    public void Archive(string archivedBy)
    {
        Status = WorkflowStatus.Archived;
        Touch(archivedBy);
    }

    /// <summary>Updates the definition JSON and increments the version.</summary>
    public void UpdateDefinition(string definitionJson, string updatedBy, string? description = null, int? timeoutMinutes = null)
    {
        _versions.Add(WorkflowVersion.Snapshot(this));

        DefinitionJson = definitionJson;
        Description = description ?? Description;
        TimeoutMinutes = timeoutMinutes ?? TimeoutMinutes;
        Version++;
        Status = WorkflowStatus.Draft;

        Touch(updatedBy);
    }

    private void ValidateStructure()
    {
        var hasStart = _nodes.Any(n => n.Type == WorkflowNodeType.Start);
        var hasEnd = _nodes.Any(n => n.Type == WorkflowNodeType.End);

        if (!hasStart)
            throw new InvalidOperationException("Workflow must have at least one Start node.");
        if (!hasEnd)
            throw new InvalidOperationException("Workflow must have at least one End node.");
    }
}
