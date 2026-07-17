namespace BillAI.RulesEngine.Domain.Entities.Workflows;

/// <summary>
/// Represents a completed execution step within a <see cref="WorkflowInstance"/>.
/// </summary>
public sealed class WorkflowExecutionStep
{
    private WorkflowExecutionStep() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid InstanceId { get; private set; }
    public string NodeId { get; private set; } = string.Empty;
    public string NodeName { get; private set; } = string.Empty;
    public bool Succeeded { get; private set; }
    public string? OutputJson { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset ExecutedAt { get; private set; } = DateTimeOffset.UtcNow;
    public long DurationMs { get; private set; }

    internal static WorkflowExecutionStep Create(
        Guid instanceId,
        string nodeId,
        string nodeName,
        bool succeeded,
        string? output,
        string? error)
        => new()
        {
            InstanceId = instanceId,
            NodeId = nodeId,
            NodeName = nodeName,
            Succeeded = succeeded,
            OutputJson = output,
            ErrorMessage = error
        };
}
