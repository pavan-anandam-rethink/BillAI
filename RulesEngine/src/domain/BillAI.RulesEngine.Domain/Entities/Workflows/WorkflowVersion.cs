namespace BillAI.RulesEngine.Domain.Entities.Workflows;

/// <summary>
/// Represents an immutable version snapshot of a <see cref="WorkflowDefinition"/>.
/// </summary>
public sealed class WorkflowVersion
{
    private WorkflowVersion() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid WorkflowId { get; private set; }
    public int VersionNumber { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string? DefinitionJson { get; private set; }
    public DateTimeOffset SnapshotAt { get; private set; }

    internal static WorkflowVersion Snapshot(WorkflowDefinition workflow) => new()
    {
        WorkflowId = workflow.Id,
        VersionNumber = workflow.Version,
        Name = workflow.Name,
        Description = workflow.Description,
        Category = workflow.Category,
        DefinitionJson = workflow.DefinitionJson,
        SnapshotAt = DateTimeOffset.UtcNow
    };
}
