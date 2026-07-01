using ClaimLifecycleMonitoring.Application.Abstractions.RuleEngine;
using ClaimLifecycleMonitoring.Application.Configuration;
using ClaimLifecycleMonitoring.Domain.Entities;
using ClaimLifecycleMonitoring.Domain.Enums;
using Microsoft.Extensions.Options;

namespace ClaimLifecycleMonitoring.Application.Monitoring.Rules;

/// <summary>Composite rule engine that fans out to a set of registered rules.</summary>
public sealed class CompositeRuleEngine : IRuleEngine
{
    private readonly IReadOnlyList<IMonitoringRule> _rules;

    /// <summary>Creates a new <see cref="CompositeRuleEngine"/>.</summary>
    public CompositeRuleEngine(IEnumerable<IMonitoringRule> rules)
    {
        _rules = rules.ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<RuleEvaluationResult> Evaluate(Claim claim, DateTime nowUtc, IReadOnlyDictionary<ClaimStage, StageSlaConfiguration> slas)
    {
        ArgumentNullException.ThrowIfNull(claim);
        var results = new List<RuleEvaluationResult>();
        foreach (var rule in _rules)
        {
            foreach (var r in rule.Evaluate(claim, nowUtc, slas))
            {
                results.Add(r);
            }
        }
        return results;
    }
}

/// <summary>Rule that flags claims stuck at their current stage beyond the configured SLA.</summary>
public sealed class StageSlaBreachRule : IMonitoringRule
{
    /// <inheritdoc />
    public string RuleId => "STAGE_SLA_BREACH";

    /// <inheritdoc />
    public IEnumerable<RuleEvaluationResult> Evaluate(Claim claim, DateTime nowUtc, IReadOnlyDictionary<ClaimStage, StageSlaConfiguration> slas)
    {
        if (IsTerminal(claim.CurrentStatus))
        {
            yield break;
        }
        var hoursInStage = (nowUtc - claim.StageEnteredUtc).TotalHours;
        var slaHours = slas.TryGetValue(claim.CurrentStage, out var cfg) ? cfg.SlaHours : claim.CurrentStageSlaHours;
        if (slaHours > 0 && hoursInStage > slaHours && !HasRecentAlert(claim, FailureCategory.SlaBreach, nowUtc))
        {
            yield return new RuleEvaluationResult
            {
                RuleId = RuleId,
                Category = FailureCategory.SlaBreach,
                Severity = AlertSeverity.Warning,
                Message = $"Claim has been in stage {claim.CurrentStage} for {hoursInStage:F1}h (SLA {slaHours}h).",
                MarkStuck = hoursInStage > slaHours * 1.5
            };
        }
    }

    private static bool HasRecentAlert(Claim claim, FailureCategory category, DateTime nowUtc)
        => claim.Alerts.Any(a => a.Category == category && a.StageAtAlert == claim.CurrentStage && (nowUtc - a.RaisedUtc).TotalHours < 6);

    private static bool IsTerminal(ClaimStatus status)
        => status is ClaimStatus.Completed or ClaimStatus.Cancelled or ClaimStatus.Duplicate;
}

/// <summary>Rule that flags claims whose global lifecycle age has exceeded the configured timeout.</summary>
public sealed class GlobalTimeoutRule : IMonitoringRule
{
    private readonly MonitoringOptions _options;

    /// <summary>Creates a new <see cref="GlobalTimeoutRule"/>.</summary>
    public GlobalTimeoutRule(IOptions<MonitoringOptions> options) { _options = options.Value; }

    /// <inheritdoc />
    public string RuleId => "GLOBAL_TIMEOUT";

    /// <inheritdoc />
    public IEnumerable<RuleEvaluationResult> Evaluate(Claim claim, DateTime nowUtc, IReadOnlyDictionary<ClaimStage, StageSlaConfiguration> slas)
    {
        if (claim.CurrentStatus is ClaimStatus.Completed or ClaimStatus.Cancelled or ClaimStatus.Duplicate)
        {
            yield break;
        }
        var age = (nowUtc - claim.CreatedUtc).TotalHours;
        if (age > _options.GlobalClaimTimeoutHours)
        {
            yield return new RuleEvaluationResult
            {
                RuleId = RuleId,
                Category = FailureCategory.Timeout,
                Severity = AlertSeverity.Critical,
                Message = $"Claim has exceeded global lifecycle timeout of {_options.GlobalClaimTimeoutHours}h (current age {age:F1}h).",
                MarkStuck = true
            };
        }
    }
}

/// <summary>Base helper for rules that watch for a missing inbound message.</summary>
public abstract class MissingInboundRuleBase : IMonitoringRule
{
    /// <inheritdoc />
    public abstract string RuleId { get; }

    /// <summary>The stage the claim must have reached to be waiting on the inbound message.</summary>
    protected abstract ClaimStage AwaitingStage { get; }

    /// <summary>How many hours may elapse before the missing message is escalated.</summary>
    protected abstract int ThresholdHours { get; }

    /// <summary>Failure category to assign when the message is missing.</summary>
    protected abstract FailureCategory Category { get; }

    /// <inheritdoc />
    public IEnumerable<RuleEvaluationResult> Evaluate(Claim claim, DateTime nowUtc, IReadOnlyDictionary<ClaimStage, StageSlaConfiguration> slas)
    {
        if (claim.CurrentStage != AwaitingStage || claim.CurrentStatus is ClaimStatus.Completed or ClaimStatus.Cancelled or ClaimStatus.Duplicate)
        {
            yield break;
        }
        var waited = (nowUtc - claim.StageEnteredUtc).TotalHours;
        if (waited > ThresholdHours && !claim.Alerts.Any(a => a.Category == Category && (nowUtc - a.RaisedUtc).TotalHours < 12))
        {
            yield return new RuleEvaluationResult
            {
                RuleId = RuleId,
                Category = Category,
                Severity = AlertSeverity.Warning,
                Message = $"{Category} : no inbound response received {waited:F1}h after entering {AwaitingStage} (threshold {ThresholdHours}h).",
                MarkStuck = waited > ThresholdHours * 2
            };
        }
    }
}

/// <summary>Rule detecting a missing 999 acknowledgement.</summary>
public sealed class Missing999Rule : MissingInboundRuleBase
{
    private readonly MonitoringOptions _options;

    /// <summary>Creates a new <see cref="Missing999Rule"/>.</summary>
    public Missing999Rule(IOptions<MonitoringOptions> options) { _options = options.Value; }

    /// <inheritdoc />
    public override string RuleId => "MISSING_999";

    /// <inheritdoc />
    protected override ClaimStage AwaitingStage => ClaimStage.EdiSubmitted837;

    /// <inheritdoc />
    protected override int ThresholdHours => _options.Expected999Hours;

    /// <inheritdoc />
    protected override FailureCategory Category => FailureCategory.Missing999;
}

/// <summary>Rule detecting a missing 277CA acknowledgement.</summary>
public sealed class Missing277CaRule : MissingInboundRuleBase
{
    private readonly MonitoringOptions _options;

    /// <summary>Creates a new <see cref="Missing277CaRule"/>.</summary>
    public Missing277CaRule(IOptions<MonitoringOptions> options) { _options = options.Value; }

    /// <inheritdoc />
    public override string RuleId => "MISSING_277CA";

    /// <inheritdoc />
    protected override ClaimStage AwaitingStage => ClaimStage.Received999;

    /// <inheritdoc />
    protected override int ThresholdHours => _options.Expected277CaHours;

    /// <inheritdoc />
    protected override FailureCategory Category => FailureCategory.Missing277CA;
}

/// <summary>Rule detecting a missing 835 remittance.</summary>
public sealed class Missing835Rule : MissingInboundRuleBase
{
    private readonly MonitoringOptions _options;

    /// <summary>Creates a new <see cref="Missing835Rule"/>.</summary>
    public Missing835Rule(IOptions<MonitoringOptions> options) { _options = options.Value; }

    /// <inheritdoc />
    public override string RuleId => "MISSING_835";

    /// <inheritdoc />
    protected override ClaimStage AwaitingStage => ClaimStage.PayerProcessing;

    /// <inheritdoc />
    protected override int ThresholdHours => _options.Expected835Hours;

    /// <inheritdoc />
    protected override FailureCategory Category => FailureCategory.Missing835;
}

/// <summary>Rule that promotes an existing failure category to an alert if one has not yet been recorded.</summary>
public sealed class UnalertedFailureRule : IMonitoringRule
{
    /// <inheritdoc />
    public string RuleId => "UNALERTED_FAILURE";

    /// <inheritdoc />
    public IEnumerable<RuleEvaluationResult> Evaluate(Claim claim, DateTime nowUtc, IReadOnlyDictionary<ClaimStage, StageSlaConfiguration> slas)
    {
        if (!claim.HasFailure || claim.FailureCategory == FailureCategory.None)
        {
            yield break;
        }
        if (claim.Alerts.Any(a => a.Category == claim.FailureCategory && (nowUtc - a.RaisedUtc).TotalHours < 24))
        {
            yield break;
        }
        yield return new RuleEvaluationResult
        {
            RuleId = RuleId,
            Category = claim.FailureCategory,
            Severity = claim.ManualInterventionRequired ? AlertSeverity.Error : AlertSeverity.Warning,
            Message = claim.ExceptionMessage ?? $"Failure recorded: {claim.FailureCategory}.",
            MarkStuck = false
        };
    }
}
