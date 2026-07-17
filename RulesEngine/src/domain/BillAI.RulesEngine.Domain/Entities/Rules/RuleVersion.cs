using BillAI.RulesEngine.Domain.Enums;

namespace BillAI.RulesEngine.Domain.Entities.Rules;

/// <summary>
/// Represents an immutable version snapshot of a <see cref="RuleDefinition"/>.
/// </summary>
public sealed class RuleVersion
{
    private RuleVersion() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RuleId { get; private set; }
    public int VersionNumber { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string RuleExpression { get; private set; } = string.Empty;
    public int Priority { get; private set; }
    public RuleSeverity Severity { get; private set; }
    public string? FailureMessage { get; private set; }
    public DateTimeOffset? EffectiveFrom { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public string? DecisionTableJson { get; private set; }
    public DateTimeOffset SnapshotAt { get; private set; }

    /// <summary>Creates a snapshot of the current rule state.</summary>
    internal static RuleVersion Snapshot(RuleDefinition rule) => new()
    {
        RuleId = rule.Id,
        VersionNumber = rule.Version,
        Name = rule.Name,
        Description = rule.Description,
        Category = rule.Category,
        RuleExpression = rule.RuleExpression,
        Priority = rule.Priority,
        Severity = rule.Severity,
        FailureMessage = rule.FailureMessage,
        EffectiveFrom = rule.EffectiveFrom,
        ExpiresAt = rule.ExpiresAt,
        DecisionTableJson = rule.DecisionTableJson,
        SnapshotAt = DateTimeOffset.UtcNow
    };
}
