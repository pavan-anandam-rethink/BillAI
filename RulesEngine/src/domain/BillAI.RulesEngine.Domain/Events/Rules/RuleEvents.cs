using BillAI.RulesEngine.Domain.Common;

namespace BillAI.RulesEngine.Domain.Events.Rules;

/// <summary>Raised when a new rule definition is created.</summary>
public sealed class RuleCreatedEvent(Guid ruleId, string tenantId, string ruleName, string createdBy)
    : DomainEvent
{
    public Guid RuleId { get; } = ruleId;
    public string TenantId { get; } = tenantId;
    public string RuleName { get; } = ruleName;
    public string CreatedBy { get; } = createdBy;
}

/// <summary>Raised when a rule definition is updated.</summary>
public sealed class RuleUpdatedEvent(Guid ruleId, string tenantId, string ruleName, int version, string updatedBy)
    : DomainEvent
{
    public Guid RuleId { get; } = ruleId;
    public string TenantId { get; } = tenantId;
    public string RuleName { get; } = ruleName;
    public int Version { get; } = version;
    public string UpdatedBy { get; } = updatedBy;
}

/// <summary>Raised when a rule is published.</summary>
public sealed class RulePublishedEvent(Guid ruleId, string tenantId, string ruleName, int version, string publishedBy)
    : DomainEvent
{
    public Guid RuleId { get; } = ruleId;
    public string TenantId { get; } = tenantId;
    public string RuleName { get; } = ruleName;
    public int Version { get; } = version;
    public string PublishedBy { get; } = publishedBy;
}

/// <summary>Raised when a rule is rolled back to a previous version.</summary>
public sealed class RuleRolledBackEvent(
    Guid ruleId,
    string tenantId,
    string ruleName,
    int fromVersion,
    int toVersion,
    string rolledBackBy)
    : DomainEvent
{
    public Guid RuleId { get; } = ruleId;
    public string TenantId { get; } = tenantId;
    public string RuleName { get; } = ruleName;
    public int FromVersion { get; } = fromVersion;
    public int ToVersion { get; } = toVersion;
    public string RolledBackBy { get; } = rolledBackBy;
}
