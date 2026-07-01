using ClaimLifecycleMonitoring.Domain.Entities;
using ClaimLifecycleMonitoring.Domain.Enums;

namespace ClaimLifecycleMonitoring.Application.Abstractions.RuleEngine;

/// <summary>
/// Represents a single evaluation result produced by the rule engine
/// against a specific claim.
/// </summary>
public sealed class RuleEvaluationResult
{
    /// <summary>Identifier of the rule that produced the result.</summary>
    public string RuleId { get; init; } = string.Empty;

    /// <summary>Failure category the rule detected.</summary>
    public FailureCategory Category { get; init; }

    /// <summary>Severity of the alert.</summary>
    public AlertSeverity Severity { get; init; }

    /// <summary>Human readable message describing the rule breach.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Whether the claim should be flagged as stuck.</summary>
    public bool MarkStuck { get; init; }
}

/// <summary>
/// Contract for individual monitoring rules. Each rule is evaluated per-claim by
/// the <see cref="IRuleEngine"/> and returns zero or more <see cref="RuleEvaluationResult"/>s.
/// </summary>
public interface IMonitoringRule
{
    /// <summary>Rule identifier used for logging and correlation.</summary>
    string RuleId { get; }

    /// <summary>Evaluates the rule against a claim.</summary>
    /// <returns>Zero or more results describing detected issues.</returns>
    IEnumerable<RuleEvaluationResult> Evaluate(Claim claim, DateTime nowUtc, IReadOnlyDictionary<ClaimStage, StageSlaConfiguration> slas);
}

/// <summary>
/// Composite rule engine that runs a set of <see cref="IMonitoringRule"/> instances against a claim.
/// </summary>
public interface IRuleEngine
{
    /// <summary>Evaluates all rules against the supplied claim.</summary>
    IReadOnlyList<RuleEvaluationResult> Evaluate(Claim claim, DateTime nowUtc, IReadOnlyDictionary<ClaimStage, StageSlaConfiguration> slas);
}
