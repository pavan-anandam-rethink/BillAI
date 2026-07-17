using System.Text.Json;
using BillAI.RulesEngine.Application.Interfaces.Services;
using BillAI.RulesEngine.Domain.Entities.Rules;
using BillAI.RulesEngine.Domain.Enums;
using BillAI.RulesEngine.Domain.ValueObjects;
using BillAI.RulesEngine.Engine.Operators;
using Microsoft.Extensions.Logging;

namespace BillAI.RulesEngine.Engine.Evaluators;

/// <summary>
/// High-performance, stateless, thread-safe rules evaluation engine.
/// Evaluates a set of published rule definitions against a data context.
/// </summary>
public sealed class RulesEvaluationEngine(
    ILogger<RulesEvaluationEngine> logger)
    : IRulesEvaluationEngine
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
    /// <inheritdoc/>
    public async Task<RuleEvaluationResult> EvaluateAsync(
        IReadOnlyList<RuleDefinition> rules,
        object dataContext,
        CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var passed = new List<RuleResult>();
        var failed = new List<RuleResult>();
        var warnings = new List<RuleResult>();
        var traceLines = new System.Text.StringBuilder();

        // Sort by priority (ascending = higher priority first)
        var ordered = rules
            .Where(r => r.IsEnabled && r.Status == RuleStatus.Published)
            .OrderBy(r => r.Priority)
            .ToList();

        var tasks = ordered.Select(rule => EvaluateSingleInternalAsync(rule, dataContext, ct));
        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            traceLines.AppendLine(result.DecisionPath);

            if (result.Passed)
                passed.Add(result);
            else if (result.Message?.Contains("warning", StringComparison.OrdinalIgnoreCase) == true)
                warnings.Add(result);
            else
                failed.Add(result);
        }

        sw.Stop();

        return new RuleEvaluationResult
        {
            IsValid = failed.Count == 0,
            PassedRules = passed,
            FailedRules = failed,
            Warnings = warnings,
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            DecisionTrace = traceLines.ToString()
        };
    }

    /// <inheritdoc/>
    public Task<RuleResult> EvaluateSingleAsync(
        RuleDefinition rule,
        object dataContext,
        CancellationToken ct = default)
        => EvaluateSingleInternalAsync(rule, dataContext, ct);

    private async Task<RuleResult> EvaluateSingleInternalAsync(
        RuleDefinition rule,
        object dataContext,
        CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        bool passed;
        string? decisionPath;

        try
        {
            var expression = JsonSerializer.Deserialize<RuleCondition>(rule.RuleExpression, _jsonOptions);
            if (expression is null)
                throw new InvalidOperationException("Rule expression is empty or invalid.");

            (passed, decisionPath) = EvaluateCondition(expression, dataContext, new System.Text.StringBuilder());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error evaluating rule {RuleId} ({RuleName})", rule.Id, rule.Name);
            passed = false;
            decisionPath = $"[ERROR] {ex.Message}";
        }

        sw.Stop();

        return new RuleResult
        {
            RuleId = rule.Id,
            RuleName = rule.Name,
            Category = rule.Category,
            Passed = passed,
            Message = passed ? null : (rule.FailureMessage ?? $"Rule '{rule.Name}' failed."),
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            DecisionPath = decisionPath
        };
    }

    private static (bool Result, string Trace) EvaluateCondition(
        RuleCondition condition,
        object dataContext,
        System.Text.StringBuilder trace)
    {
        if (condition.IsComposite)
            return EvaluateComposite(condition, dataContext, trace);

        // Leaf condition
        var fieldValue = condition.Field is not null
            ? FieldExtractor.Extract(dataContext, condition.Field)
            : null;

        var result = ConditionEvaluator.Evaluate(
            fieldValue,
            condition.Operator,
            condition.Value,
            condition.DataType);

        var traceEntry = $"{condition.Field} {condition.Operator} {condition.Value} => {result}";
        trace.AppendLine(traceEntry);

        return (result, traceEntry);
    }

    private static (bool Result, string Trace) EvaluateComposite(
        RuleCondition condition,
        object dataContext,
        System.Text.StringBuilder trace)
    {
        var childResults = condition.Children
            .Select(c => EvaluateCondition(c, dataContext, trace))
            .ToList();

        var result = condition.LogicalOperator switch
        {
            LogicalOperator.And => childResults.All(r => r.Result),
            LogicalOperator.Or => childResults.Any(r => r.Result),
            LogicalOperator.Not => childResults.Count == 1 && !childResults[0].Result,
            _ => false
        };

        trace.AppendLine($"[{condition.LogicalOperator}] => {result}");
        return (result, trace.ToString());
    }
}
