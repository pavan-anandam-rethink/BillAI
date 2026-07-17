using BillAI.RulesEngine.Application.Interfaces.Services;
using BillAI.RulesEngine.Shared.Models;
using MediatR;

namespace BillAI.RulesEngine.Application.Claims.Commands;

/// <summary>Command to validate a healthcare claim against published rules.</summary>
public sealed record ValidateClaimCommand(
    string TenantId,
    string ClaimId,
    string ClaimJson,
    string RequestedBy,
    string? Category = null) : IRequest<Result<ClaimValidationResponse>>;

/// <summary>The result returned by the claim validation endpoint.</summary>
public sealed class ClaimValidationResponse
{
    public Guid AuditId { get; init; }
    public bool IsValid { get; init; }
    public IReadOnlyList<RuleResult> PassedRules { get; init; } = [];
    public IReadOnlyList<RuleResult> FailedRules { get; init; } = [];
    public IReadOnlyList<RuleResult> Warnings { get; init; } = [];
    public long ExecutionTimeMs { get; init; }
    public string? DecisionTrace { get; init; }
}
