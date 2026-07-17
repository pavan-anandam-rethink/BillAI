using BillAI.RulesEngine.Domain.Entities.Rules;
using BillAI.RulesEngine.Domain.Enums;
using BillAI.RulesEngine.Shared.Models;
using MediatR;

namespace BillAI.RulesEngine.Application.Rules.Queries;

// ─────────────────────────────────────────────
// Get Rule By Id
// ─────────────────────────────────────────────

/// <summary>Query to retrieve a single rule by ID.</summary>
public sealed record GetRuleByIdQuery(Guid RuleId, string TenantId) : IRequest<RuleDefinition?>;

// ─────────────────────────────────────────────
// Get Rules (paged)
// ─────────────────────────────────────────────

/// <summary>Query to retrieve a paginated list of rules.</summary>
public sealed record GetRulesQuery(
    string TenantId,
    int Page = 1,
    int PageSize = 20,
    RuleStatus? Status = null,
    string? Category = null,
    string? SearchTerm = null) : IRequest<PagedResult<RuleDefinition>>;

// ─────────────────────────────────────────────
// Get Rule Versions
// ─────────────────────────────────────────────

/// <summary>Query to retrieve the version history of a rule.</summary>
public sealed record GetRuleVersionsQuery(Guid RuleId, string TenantId) : IRequest<IReadOnlyList<RuleVersion>>;
