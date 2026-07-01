using ClaimLifecycleMonitoring.Domain.Entities;
using ClaimLifecycleMonitoring.Domain.Enums;
using ClaimLifecycleMonitoring.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace ClaimLifecycleMonitoring.Tests.Domain;

public class ClaimAggregateTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Claim NewClaim(decimal billed = 1000m) =>
        Claim.Create("CLM-1", "CUST1", "Customer One", "ACC1", "Account 1",
            "PAT-1", "Alex Patient", "PROV-1", "Dr Smith", billed, 24, Now);

    [Fact]
    public void Create_initializes_claim_in_appointment_created_stage()
    {
        var claim = NewClaim();
        claim.CurrentStage.Should().Be(ClaimStage.AppointmentCreated);
        claim.CurrentStatus.Should().Be(ClaimStatus.InProgress);
        claim.NextExpectedStage.Should().Be(ClaimStage.EligibilityVerification);
        claim.History.Should().HaveCount(1);
    }

    [Fact]
    public void Create_rejects_blank_claim_number()
    {
        var act = () => Claim.Create(" ", "c", "c", "a", "a", "p", "p", "pr", "pr", 100m, 24, Now);
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Advance_moves_stage_forward_and_records_history()
    {
        var claim = NewClaim();
        claim.AdvanceToStage(ClaimStage.EligibilityVerification, ClaimStage.Authorization, 12, null, Now.AddMinutes(5));
        claim.CurrentStage.Should().Be(ClaimStage.EligibilityVerification);
        claim.LastSuccessfulStage.Should().Be(ClaimStage.AppointmentCreated);
        claim.History.Should().HaveCount(2);
    }

    [Fact]
    public void Advance_rejects_backwards_transitions()
    {
        var claim = NewClaim();
        claim.AdvanceToStage(ClaimStage.EligibilityVerification, ClaimStage.Authorization, 12, null, Now.AddMinutes(5));
        var act = () => claim.AdvanceToStage(ClaimStage.AppointmentCreated, null, 12, null, Now.AddMinutes(10));
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void RecordFailure_flags_claim_and_categorizes_status()
    {
        var claim = NewClaim();
        claim.RecordFailure(FailureCategory.EligibilityFailure, "denied", retryAvailable: true, manualInterventionRequired: false, Now.AddMinutes(1));
        claim.HasFailure.Should().BeTrue();
        claim.FailureCategory.Should().Be(FailureCategory.EligibilityFailure);
        claim.RetryAvailable.Should().BeTrue();
        claim.CurrentStatus.Should().Be(ClaimStatus.Failed);
    }

    [Fact]
    public void RecordRetry_requires_retry_available()
    {
        var claim = NewClaim();
        var act = () => claim.RecordRetry("retry", Now.AddMinutes(1));
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void RecordRetry_increments_retry_count_and_clears_failure()
    {
        var claim = NewClaim();
        claim.RecordFailure(FailureCategory.EligibilityFailure, "denied", retryAvailable: true, manualInterventionRequired: false, Now.AddMinutes(1));
        claim.RecordRetry("attempting retry", Now.AddMinutes(2));
        claim.RetryCount.Should().Be(1);
        claim.HasFailure.Should().BeFalse();
        claim.CurrentStatus.Should().Be(ClaimStatus.Reprocessed);
    }

    [Fact]
    public void ApplyPayment_completes_claim_when_fully_paid()
    {
        var claim = NewClaim(500m);
        claim.ApplyPayment(500m, Now.AddHours(1));
        claim.PaidAmount.Should().Be(500m);
        claim.CurrentStatus.Should().Be(ClaimStatus.Completed);
    }

    [Fact]
    public void ApplyPayment_partial_payment_marks_pending_payment()
    {
        var claim = NewClaim(1000m);
        claim.ApplyPayment(400m, Now.AddHours(1));
        claim.CurrentStatus.Should().Be(ClaimStatus.PendingPayment);
    }

    [Fact]
    public void Cancel_sets_terminal_status()
    {
        var claim = NewClaim();
        claim.Cancel("patient withdrew", Now.AddHours(1));
        claim.CurrentStatus.Should().Be(ClaimStatus.Cancelled);
        claim.FailureCategory.Should().Be(FailureCategory.CancelledClaim);
    }

    [Fact]
    public void MarkDuplicate_flags_manual_intervention()
    {
        var claim = NewClaim();
        claim.MarkDuplicate("CLM-42", Now.AddHours(1));
        claim.CurrentStatus.Should().Be(ClaimStatus.Duplicate);
        claim.ManualInterventionRequired.Should().BeTrue();
    }
}
