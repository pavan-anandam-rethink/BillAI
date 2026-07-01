using ClaimLifecycleMonitoring.Application.Configuration;
using ClaimLifecycleMonitoring.Application.Monitoring.Rules;
using ClaimLifecycleMonitoring.Domain.Entities;
using ClaimLifecycleMonitoring.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ClaimLifecycleMonitoring.Tests.Monitoring;

public class MonitoringRulesTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly IReadOnlyDictionary<ClaimStage, StageSlaConfiguration> EmptySlas =
        new Dictionary<ClaimStage, StageSlaConfiguration>();

    private static IOptions<MonitoringOptions> Options(MonitoringOptions o) => Microsoft.Extensions.Options.Options.Create(o);

    private static Claim ClaimAtStage(ClaimStage stage, DateTime stageEnteredUtc)
    {
        var claim = Claim.Create("CLM-1", "C", "C", "A", "A", "P", "P", "PR", "PR", 100m, 24, stageEnteredUtc);
        if (stage != ClaimStage.AppointmentCreated)
        {
            claim.AdvanceToStage(stage, null, 24, "advance for test", stageEnteredUtc);
        }
        return claim;
    }

    [Fact]
    public void StageSlaBreachRule_flags_claim_past_stage_sla()
    {
        var claim = ClaimAtStage(ClaimStage.ClaimValidation, Now.AddHours(-48));
        var rule = new StageSlaBreachRule();
        var results = rule.Evaluate(claim, Now, EmptySlas).ToList();
        results.Should().ContainSingle();
        results[0].Category.Should().Be(FailureCategory.SlaBreach);
        results[0].MarkStuck.Should().BeTrue(); // 48h > 24 * 1.5
    }

    [Fact]
    public void StageSlaBreachRule_does_not_fire_within_sla()
    {
        var claim = ClaimAtStage(ClaimStage.ClaimValidation, Now.AddHours(-1));
        var rule = new StageSlaBreachRule();
        rule.Evaluate(claim, Now, EmptySlas).Should().BeEmpty();
    }

    [Fact]
    public void GlobalTimeoutRule_fires_when_claim_older_than_configured_timeout()
    {
        var claim = ClaimAtStage(ClaimStage.PayerProcessing, Now.AddHours(-1000));
        var rule = new GlobalTimeoutRule(Options(new MonitoringOptions { GlobalClaimTimeoutHours = 720 }));
        var results = rule.Evaluate(claim, Now, EmptySlas).ToList();
        results.Should().ContainSingle();
        results[0].Category.Should().Be(FailureCategory.Timeout);
        results[0].MarkStuck.Should().BeTrue();
    }

    [Fact]
    public void Missing999Rule_fires_when_837_submitted_too_long_ago()
    {
        var claim = ClaimAtStage(ClaimStage.EdiSubmitted837, Now.AddHours(-48));
        var rule = new Missing999Rule(Options(new MonitoringOptions { Expected999Hours = 24 }));
        var results = rule.Evaluate(claim, Now, EmptySlas).ToList();
        results.Should().ContainSingle();
        results[0].Category.Should().Be(FailureCategory.Missing999);
    }

    [Fact]
    public void Missing835Rule_ignores_completed_claims()
    {
        var claim = ClaimAtStage(ClaimStage.PayerProcessing, Now.AddHours(-2000));
        claim.AdvanceToStage(ClaimStage.Completed, null, 0, "done", Now);
        var rule = new Missing835Rule(Options(new MonitoringOptions { Expected835Hours = 720 }));
        rule.Evaluate(claim, Now, EmptySlas).Should().BeEmpty();
    }

    [Fact]
    public void UnalertedFailureRule_fires_on_failure_without_recent_alert()
    {
        var claim = ClaimAtStage(ClaimStage.ClaimValidation, Now.AddHours(-1));
        claim.RecordFailure(FailureCategory.ValidationFailure, "missing NPI", retryAvailable: false, manualInterventionRequired: true, Now);
        var rule = new UnalertedFailureRule();
        var results = rule.Evaluate(claim, Now, EmptySlas).ToList();
        results.Should().ContainSingle();
        results[0].Severity.Should().Be(AlertSeverity.Error);
    }
}
