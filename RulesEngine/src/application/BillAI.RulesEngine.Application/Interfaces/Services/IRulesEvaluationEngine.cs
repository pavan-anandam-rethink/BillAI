using BillAI.RulesEngine.Domain.Entities.Rules;

namespace BillAI.RulesEngine.Application.Interfaces.Services;

/// <summary>
/// Contract for the rules runtime engine that evaluates rule expressions against data.
/// </summary>
public interface IRulesEvaluationEngine
{
    /// <summary>
    /// Evaluates all applicable rules against the provided data context.
    /// </summary>
    /// <param name="rules">The published rules to evaluate.</param>
    /// <param name="dataContext">The data object to evaluate against (deserialised to a dictionary or JsonElement).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<RuleEvaluationResult> EvaluateAsync(
        IReadOnlyList<RuleDefinition> rules,
        object dataContext,
        CancellationToken ct = default);

    /// <summary>
    /// Evaluates a single rule against the provided data context (for simulation/testing).
    /// </summary>
    Task<RuleResult> EvaluateSingleAsync(
        RuleDefinition rule,
        object dataContext,
        CancellationToken ct = default);
}

/// <summary>
/// Aggregated result of evaluating a set of rules.
/// </summary>
public sealed class RuleEvaluationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<RuleResult> PassedRules { get; init; } = [];
    public IReadOnlyList<RuleResult> FailedRules { get; init; } = [];
    public IReadOnlyList<RuleResult> Warnings { get; init; } = [];
    public long ExecutionTimeMs { get; init; }
    public string? DecisionTrace { get; init; }
}

/// <summary>
/// Result of evaluating a single rule.
/// </summary>
public sealed class RuleResult
{
    public Guid RuleId { get; init; }
    public string RuleName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public bool Passed { get; init; }
    public string? Message { get; init; }
    public long ExecutionTimeMs { get; init; }
    public string? DecisionPath { get; init; }
}
