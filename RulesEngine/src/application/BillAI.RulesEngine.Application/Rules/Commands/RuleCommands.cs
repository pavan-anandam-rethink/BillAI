using BillAI.RulesEngine.Domain.Enums;
using BillAI.RulesEngine.Shared.Models;
using MediatR;

namespace BillAI.RulesEngine.Application.Rules.Commands;

// ─────────────────────────────────────────────
// Create Rule
// ─────────────────────────────────────────────

/// <summary>Command to create a new draft rule definition.</summary>
public sealed record CreateRuleCommand(
    string TenantId,
    string Name,
    string Category,
    string RuleExpression,
    string CreatedBy,
    string? Description = null,
    int Priority = 100,
    RuleSeverity Severity = RuleSeverity.Error,
    string? FailureMessage = null,
    DateTimeOffset? EffectiveFrom = null,
    DateTimeOffset? ExpiresAt = null) : IRequest<Result<Guid>>;

// ─────────────────────────────────────────────
// Update Rule
// ─────────────────────────────────────────────

/// <summary>Command to update an existing rule definition.</summary>
public sealed record UpdateRuleCommand(
    Guid RuleId,
    string TenantId,
    string Name,
    string Category,
    string RuleExpression,
    string UpdatedBy,
    string? Description = null,
    int Priority = 100,
    RuleSeverity Severity = RuleSeverity.Error,
    string? FailureMessage = null,
    DateTimeOffset? EffectiveFrom = null,
    DateTimeOffset? ExpiresAt = null,
    string? DecisionTableJson = null) : IRequest<Result>;

// ─────────────────────────────────────────────
// Publish Rule
// ─────────────────────────────────────────────

/// <summary>Command to publish a rule, making it active for execution.</summary>
public sealed record PublishRuleCommand(
    Guid RuleId,
    string TenantId,
    string PublishedBy) : IRequest<Result>;

// ─────────────────────────────────────────────
// Archive Rule
// ─────────────────────────────────────────────

/// <summary>Command to archive a rule.</summary>
public sealed record ArchiveRuleCommand(
    Guid RuleId,
    string TenantId,
    string ArchivedBy) : IRequest<Result>;

// ─────────────────────────────────────────────
// Delete Rule
// ─────────────────────────────────────────────

/// <summary>Command to permanently delete a rule.</summary>
public sealed record DeleteRuleCommand(
    Guid RuleId,
    string TenantId,
    string DeletedBy) : IRequest<Result>;

// ─────────────────────────────────────────────
// Rollback Rule
// ─────────────────────────────────────────────

/// <summary>Command to roll back a rule to a specific version.</summary>
public sealed record RollbackRuleCommand(
    Guid RuleId,
    string TenantId,
    int TargetVersion,
    string RolledBackBy) : IRequest<Result>;

// ─────────────────────────────────────────────
// Toggle Rule
// ─────────────────────────────────────────────

/// <summary>Command to enable or disable a rule.</summary>
public sealed record ToggleRuleCommand(
    Guid RuleId,
    string TenantId,
    bool Enable,
    string UpdatedBy) : IRequest<Result>;
