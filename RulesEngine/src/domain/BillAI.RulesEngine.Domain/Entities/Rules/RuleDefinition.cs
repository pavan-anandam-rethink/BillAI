using BillAI.RulesEngine.Domain.Common;
using BillAI.RulesEngine.Domain.Enums;
using BillAI.RulesEngine.Domain.Events.Rules;

namespace BillAI.RulesEngine.Domain.Entities.Rules;

/// <summary>
/// Represents a business rule definition with versioning and lifecycle management.
/// </summary>
public sealed class RuleDefinition : AuditableEntity
{
    private readonly List<DomainEvent> _domainEvents = [];
    private readonly List<RuleTag> _tags = [];
    private readonly List<RuleVersion> _versions = [];

    private RuleDefinition() { }

    /// <summary>Gets the unique name of the rule within the tenant.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the human-readable description of the rule.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the category this rule belongs to.</summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>Gets the current lifecycle status.</summary>
    public RuleStatus Status { get; private set; } = RuleStatus.Draft;

    /// <summary>Gets the current version number.</summary>
    public int Version { get; private set; } = 1;

    /// <summary>Gets the evaluation priority (lower = higher priority).</summary>
    public int Priority { get; private set; } = 100;

    /// <summary>Gets the date from which this rule is effective.</summary>
    public DateTimeOffset? EffectiveFrom { get; private set; }

    /// <summary>Gets the date after which this rule expires.</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>Gets whether this rule is currently enabled.</summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>Gets the serialised rule expression (JSON).</summary>
    public string RuleExpression { get; private set; } = string.Empty;

    /// <summary>Gets the rule severity when the rule fails.</summary>
    public RuleSeverity Severity { get; private set; } = RuleSeverity.Error;

    /// <summary>Gets the message template used when the rule fails.</summary>
    public string? FailureMessage { get; private set; }

    /// <summary>Gets an optional structured decision table definition (JSON).</summary>
    public string? DecisionTableJson { get; private set; }

    /// <summary>Gets the tags associated with this rule.</summary>
    public IReadOnlyList<RuleTag> Tags => _tags.AsReadOnly();

    /// <summary>Gets the version history of this rule.</summary>
    public IReadOnlyList<RuleVersion> Versions => _versions.AsReadOnly();

    /// <summary>Pops and clears all pending domain events.</summary>
    public IReadOnlyList<DomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }

    /// <summary>Factory method to create a new draft rule.</summary>
    public static RuleDefinition Create(
        string tenantId,
        string name,
        string category,
        string ruleExpression,
        string createdBy,
        string? description = null,
        int priority = 100,
        RuleSeverity severity = RuleSeverity.Error,
        string? failureMessage = null,
        DateTimeOffset? effectiveFrom = null,
        DateTimeOffset? expiresAt = null)
    {
        var rule = new RuleDefinition
        {
            TenantId = tenantId,
            Name = name,
            Description = description,
            Category = category,
            RuleExpression = ruleExpression,
            Priority = priority,
            Severity = severity,
            FailureMessage = failureMessage,
            EffectiveFrom = effectiveFrom,
            ExpiresAt = expiresAt,
            CreatedBy = createdBy,
            Status = RuleStatus.Draft
        };

        rule._domainEvents.Add(new RuleCreatedEvent(rule.Id, tenantId, name, createdBy));
        return rule;
    }

    /// <summary>Updates the rule content and bumps the version.</summary>
    public void Update(
        string name,
        string category,
        string ruleExpression,
        string updatedBy,
        string? description = null,
        int priority = 100,
        RuleSeverity severity = RuleSeverity.Error,
        string? failureMessage = null,
        DateTimeOffset? effectiveFrom = null,
        DateTimeOffset? expiresAt = null,
        string? decisionTableJson = null)
    {
        // Snapshot current version before update
        _versions.Add(RuleVersion.Snapshot(this));

        Name = name;
        Description = description;
        Category = category;
        RuleExpression = ruleExpression;
        Priority = priority;
        Severity = severity;
        FailureMessage = failureMessage;
        EffectiveFrom = effectiveFrom;
        ExpiresAt = expiresAt;
        DecisionTableJson = decisionTableJson;
        Version++;

        Touch(updatedBy);
        _domainEvents.Add(new RuleUpdatedEvent(Id, TenantId, name, Version, updatedBy));
    }

    /// <summary>Publishes the rule, making it available for execution.</summary>
    public void Publish(string publishedBy)
    {
        if (Status == RuleStatus.Published)
            return;

        Status = RuleStatus.Published;
        Touch(publishedBy);
        _domainEvents.Add(new RulePublishedEvent(Id, TenantId, Name, Version, publishedBy));
    }

    /// <summary>Archives the rule, preventing further execution.</summary>
    public void Archive(string archivedBy)
    {
        Status = RuleStatus.Archived;
        IsEnabled = false;
        Touch(archivedBy);
    }

    /// <summary>Enables the rule for execution.</summary>
    public void Enable(string updatedBy)
    {
        IsEnabled = true;
        Touch(updatedBy);
    }

    /// <summary>Disables the rule without archiving it.</summary>
    public void Disable(string updatedBy)
    {
        IsEnabled = false;
        Touch(updatedBy);
    }

    /// <summary>Rolls back the rule to the previous version snapshot.</summary>
    public void Rollback(int targetVersion, string rolledBackBy)
    {
        var snapshot = _versions.FirstOrDefault(v => v.VersionNumber == targetVersion)
            ?? throw new InvalidOperationException($"Version {targetVersion} not found for rule {Id}.");

        // Snapshot current before rollback
        _versions.Add(RuleVersion.Snapshot(this));

        Name = snapshot.Name;
        Description = snapshot.Description;
        Category = snapshot.Category;
        RuleExpression = snapshot.RuleExpression;
        Priority = snapshot.Priority;
        Severity = snapshot.Severity;
        FailureMessage = snapshot.FailureMessage;
        EffectiveFrom = snapshot.EffectiveFrom;
        ExpiresAt = snapshot.ExpiresAt;
        DecisionTableJson = snapshot.DecisionTableJson;
        Version++;
        Status = RuleStatus.Draft;

        Touch(rolledBackBy);
        _domainEvents.Add(new RuleRolledBackEvent(Id, TenantId, Name, targetVersion, Version, rolledBackBy));
    }

    /// <summary>Adds a tag to the rule.</summary>
    public void AddTag(string tag)
    {
        if (!_tags.Any(t => t.Value.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            _tags.Add(new RuleTag(tag));
    }

    /// <summary>Removes a tag from the rule.</summary>
    public void RemoveTag(string tag)
    {
        var existing = _tags.FirstOrDefault(t => t.Value.Equals(tag, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            _tags.Remove(existing);
    }
}
