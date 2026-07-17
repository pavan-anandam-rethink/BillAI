using BillAI.RulesEngine.Domain.Enums;

namespace BillAI.RulesEngine.Domain.Entities.Workflows;

/// <summary>
/// Represents a single node in a workflow graph.
/// </summary>
public sealed class WorkflowNode
{
    private WorkflowNode() { }

    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>Gets the workflow-scoped unique identifier for this node.</summary>
    public string NodeId { get; private set; } = string.Empty;

    public Guid WorkflowId { get; private set; }
    public WorkflowNodeType Type { get; private set; }
    public string Label { get; private set; } = string.Empty;

    /// <summary>Gets optional JSON configuration specific to this node type.</summary>
    public string? ConfigJson { get; private set; }

    /// <summary>Gets the X canvas position.</summary>
    public double PositionX { get; private set; }

    /// <summary>Gets the Y canvas position.</summary>
    public double PositionY { get; private set; }

    internal static WorkflowNode Create(Guid workflowId, string nodeId, WorkflowNodeType type, string label, string? configJson)
        => new()
        {
            WorkflowId = workflowId,
            NodeId = nodeId,
            Type = type,
            Label = label,
            ConfigJson = configJson
        };

    public void SetPosition(double x, double y)
    {
        PositionX = x;
        PositionY = y;
    }
}

/// <summary>
/// Represents a directed edge (transition) between two workflow nodes.
/// </summary>
public sealed class WorkflowTransition
{
    private WorkflowTransition() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid WorkflowId { get; private set; }
    public string SourceNodeId { get; private set; } = string.Empty;
    public string TargetNodeId { get; private set; } = string.Empty;

    /// <summary>Gets an optional condition expression that controls traversal.</summary>
    public string? Condition { get; private set; }

    /// <summary>Gets an optional human-readable label for this transition.</summary>
    public string? Label { get; private set; }

    internal static WorkflowTransition Create(Guid workflowId, string source, string target, string? condition, string? label)
        => new()
        {
            WorkflowId = workflowId,
            SourceNodeId = source,
            TargetNodeId = target,
            Condition = condition,
            Label = label
        };
}
