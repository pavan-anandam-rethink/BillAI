using ClaimLifecycleMonitoring.Domain.Common;
using ClaimLifecycleMonitoring.Domain.Enums;
using ClaimLifecycleMonitoring.Domain.Exceptions;

namespace ClaimLifecycleMonitoring.Domain.Entities;

/// <summary>
/// Aggregate root representing a single healthcare claim under monitoring.
/// A <see cref="Claim"/> owns its lifecycle history, alerts and computed status.
/// </summary>
public sealed class Claim : EntityBase
{
    private readonly List<ClaimLifecycleEvent> _history = new();
    private readonly List<ClaimAlert> _alerts = new();

    /// <summary>Unique claim number assigned by the source billing system.</summary>
    public string ClaimNumber { get; private set; } = string.Empty;

    /// <summary>Customer (tenant) that owns the claim.</summary>
    public string CustomerCode { get; private set; } = string.Empty;

    /// <summary>Human readable customer name.</summary>
    public string CustomerName { get; private set; } = string.Empty;

    /// <summary>Account (payer / plan) associated with the claim.</summary>
    public string AccountCode { get; private set; } = string.Empty;

    /// <summary>Human readable account name.</summary>
    public string AccountName { get; private set; } = string.Empty;

    /// <summary>Patient identifier as known to the source system.</summary>
    public string PatientId { get; private set; } = string.Empty;

    /// <summary>Patient display name (de-identified when possible).</summary>
    public string PatientName { get; private set; } = string.Empty;

    /// <summary>Provider (rendering / billing) identifier.</summary>
    public string ProviderId { get; private set; } = string.Empty;

    /// <summary>Provider display name.</summary>
    public string ProviderName { get; private set; } = string.Empty;

    /// <summary>The stage the claim is currently in.</summary>
    public ClaimStage CurrentStage { get; private set; }

    /// <summary>The status of the claim within its current stage.</summary>
    public ClaimStatus CurrentStatus { get; private set; }

    /// <summary>The last stage that completed successfully.</summary>
    public ClaimStage? LastSuccessfulStage { get; private set; }

    /// <summary>The next stage the claim is expected to advance to.</summary>
    public ClaimStage? NextExpectedStage { get; private set; }

    /// <summary>UTC timestamp when the claim entered the current stage.</summary>
    public DateTime StageEnteredUtc { get; private set; }

    /// <summary>UTC timestamp when the claim was submitted to the payer/clearinghouse.</summary>
    public DateTime? SubmissionDateUtc { get; private set; }

    /// <summary>Whether the most recent step failed.</summary>
    public bool HasFailure { get; private set; }

    /// <summary>Category of failure detected, if any.</summary>
    public FailureCategory FailureCategory { get; private set; } = FailureCategory.None;

    /// <summary>The exception message associated with the most recent failure.</summary>
    public string? ExceptionMessage { get; private set; }

    /// <summary>Whether the claim can be retried automatically.</summary>
    public bool RetryAvailable { get; private set; }

    /// <summary>How many times the claim has been retried.</summary>
    public int RetryCount { get; private set; }

    /// <summary>Whether the claim requires manual intervention.</summary>
    public bool ManualInterventionRequired { get; private set; }

    /// <summary>Total monetary amount billed on the claim.</summary>
    public decimal BilledAmount { get; private set; }

    /// <summary>Amount paid to date.</summary>
    public decimal PaidAmount { get; private set; }

    /// <summary>SLA (in hours) permitted for the current stage before the claim is flagged stuck.</summary>
    public int CurrentStageSlaHours { get; private set; }

    /// <summary>Full ordered history of lifecycle events for this claim.</summary>
    public IReadOnlyCollection<ClaimLifecycleEvent> History => _history.AsReadOnly();

    /// <summary>Alerts raised by the monitoring engine against this claim.</summary>
    public IReadOnlyCollection<ClaimAlert> Alerts => _alerts.AsReadOnly();

    /// <summary>
    /// The age of the claim from creation to now.
    /// </summary>
    /// <param name="nowUtc">The current UTC time used for the calculation.</param>
    /// <returns>The claim age as a <see cref="TimeSpan"/>.</returns>
    public TimeSpan Age(DateTime nowUtc) => nowUtc - CreatedUtc;

    /// <summary>EF Core / persistence constructor.</summary>
    private Claim() { }

    /// <summary>
    /// Creates a new <see cref="Claim"/> in the initial <see cref="ClaimStage.AppointmentCreated"/> stage.
    /// </summary>
    public static Claim Create(
        string claimNumber,
        string customerCode,
        string customerName,
        string accountCode,
        string accountName,
        string patientId,
        string patientName,
        string providerId,
        string providerName,
        decimal billedAmount,
        int initialStageSlaHours,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(claimNumber))
        {
            throw new DomainValidationException("Claim number is required.");
        }

        if (billedAmount < 0)
        {
            throw new DomainValidationException("Billed amount cannot be negative.");
        }

        if (initialStageSlaHours <= 0)
        {
            throw new DomainValidationException("Initial stage SLA must be positive.");
        }

        var claim = new Claim
        {
            ClaimNumber = claimNumber.Trim(),
            CustomerCode = customerCode?.Trim() ?? string.Empty,
            CustomerName = customerName?.Trim() ?? string.Empty,
            AccountCode = accountCode?.Trim() ?? string.Empty,
            AccountName = accountName?.Trim() ?? string.Empty,
            PatientId = patientId?.Trim() ?? string.Empty,
            PatientName = patientName?.Trim() ?? string.Empty,
            ProviderId = providerId?.Trim() ?? string.Empty,
            ProviderName = providerName?.Trim() ?? string.Empty,
            BilledAmount = billedAmount,
            CurrentStage = ClaimStage.AppointmentCreated,
            CurrentStatus = ClaimStatus.InProgress,
            NextExpectedStage = ClaimStage.EligibilityVerification,
            StageEnteredUtc = nowUtc,
            CreatedUtc = nowUtc,
            CurrentStageSlaHours = initialStageSlaHours
        };

        claim._history.Add(ClaimLifecycleEvent.Create(
            ClaimStage.AppointmentCreated,
            StepOutcome.Success,
            "Claim created and lifecycle monitoring initiated.",
            null,
            nowUtc));

        return claim;
    }

    /// <summary>
    /// Advances the claim to the given <paramref name="stage"/> and records a successful history event.
    /// </summary>
    public void AdvanceToStage(ClaimStage stage, ClaimStage? nextExpectedStage, int slaHoursForNextStage, string? message, DateTime nowUtc)
    {
        if ((int)stage <= (int)CurrentStage && CurrentStatus == ClaimStatus.InProgress)
        {
            throw new DomainValidationException(
                $"Cannot advance claim '{ClaimNumber}' backwards from {CurrentStage} to {stage}.");
        }

        LastSuccessfulStage = CurrentStage;
        CurrentStage = stage;
        CurrentStatus = stage == ClaimStage.Completed ? ClaimStatus.Completed : ClaimStatus.InProgress;
        NextExpectedStage = nextExpectedStage;
        StageEnteredUtc = nowUtc;
        HasFailure = false;
        FailureCategory = FailureCategory.None;
        ExceptionMessage = null;
        ManualInterventionRequired = false;
        RetryAvailable = false;
        CurrentStageSlaHours = slaHoursForNextStage > 0 ? slaHoursForNextStage : CurrentStageSlaHours;
        ModifiedUtc = nowUtc;

        if (stage == ClaimStage.EdiSubmitted837 && SubmissionDateUtc is null)
        {
            SubmissionDateUtc = nowUtc;
        }

        _history.Add(ClaimLifecycleEvent.Create(
            stage,
            stage == ClaimStage.Completed ? StepOutcome.Success : StepOutcome.Success,
            message ?? $"Advanced to stage {stage}.",
            null,
            nowUtc));
    }

    /// <summary>
    /// Records a failure at the current stage.
    /// </summary>
    public void RecordFailure(FailureCategory category, string exceptionMessage, bool retryAvailable, bool manualInterventionRequired, DateTime nowUtc)
    {
        if (category == FailureCategory.None)
        {
            throw new DomainValidationException("Failure category must be specified when recording a failure.");
        }

        HasFailure = true;
        FailureCategory = category;
        ExceptionMessage = exceptionMessage;
        RetryAvailable = retryAvailable;
        ManualInterventionRequired = manualInterventionRequired;
        CurrentStatus = manualInterventionRequired ? ClaimStatus.ManualActionRequired : ClaimStatus.Failed;
        ModifiedUtc = nowUtc;

        _history.Add(ClaimLifecycleEvent.Create(
            CurrentStage,
            StepOutcome.Failure,
            $"Failure in stage {CurrentStage}: {category}",
            exceptionMessage,
            nowUtc));
    }

    /// <summary>
    /// Marks the claim as stuck due to an SLA breach at the current stage.
    /// </summary>
    public void MarkStuck(FailureCategory category, string message, DateTime nowUtc)
    {
        HasFailure = true;
        FailureCategory = category;
        ExceptionMessage = message;
        CurrentStatus = ClaimStatus.Stuck;
        ManualInterventionRequired = true;
        RetryAvailable = false;
        ModifiedUtc = nowUtc;

        _history.Add(ClaimLifecycleEvent.Create(
            CurrentStage,
            StepOutcome.Timeout,
            message,
            null,
            nowUtc));
    }

    /// <summary>
    /// Marks the claim as waiting for an external event (e.g. payer response).
    /// </summary>
    public void MarkWaiting(string message, DateTime nowUtc)
    {
        CurrentStatus = ClaimStatus.Waiting;
        ModifiedUtc = nowUtc;
        _history.Add(ClaimLifecycleEvent.Create(CurrentStage, StepOutcome.InProgress, message, null, nowUtc));
    }

    /// <summary>
    /// Records a successful retry attempt and returns the claim to the in-progress state.
    /// </summary>
    public void RecordRetry(string message, DateTime nowUtc)
    {
        if (!RetryAvailable)
        {
            throw new DomainValidationException("Retry is not available for the current claim state.");
        }

        RetryCount++;
        HasFailure = false;
        FailureCategory = FailureCategory.None;
        ExceptionMessage = null;
        CurrentStatus = ClaimStatus.Reprocessed;
        ManualInterventionRequired = false;
        ModifiedUtc = nowUtc;

        _history.Add(ClaimLifecycleEvent.Create(CurrentStage, StepOutcome.Retried, message, null, nowUtc));
    }

    /// <summary>
    /// Cancels the claim; no further processing will occur.
    /// </summary>
    public void Cancel(string reason, DateTime nowUtc)
    {
        CurrentStatus = ClaimStatus.Cancelled;
        HasFailure = false;
        RetryAvailable = false;
        ManualInterventionRequired = false;
        FailureCategory = FailureCategory.CancelledClaim;
        ExceptionMessage = reason;
        ModifiedUtc = nowUtc;

        _history.Add(ClaimLifecycleEvent.Create(CurrentStage, StepOutcome.Skipped, $"Claim cancelled: {reason}", null, nowUtc));
    }

    /// <summary>
    /// Marks the claim as a duplicate of another claim.
    /// </summary>
    public void MarkDuplicate(string duplicateOfClaimNumber, DateTime nowUtc)
    {
        CurrentStatus = ClaimStatus.Duplicate;
        FailureCategory = FailureCategory.DuplicateClaim;
        ExceptionMessage = $"Duplicate of claim {duplicateOfClaimNumber}.";
        ManualInterventionRequired = true;
        RetryAvailable = false;
        ModifiedUtc = nowUtc;

        _history.Add(ClaimLifecycleEvent.Create(CurrentStage, StepOutcome.Failure,
            $"Duplicate of claim {duplicateOfClaimNumber}.", null, nowUtc));
    }

    /// <summary>
    /// Applies a payment to the claim and updates the outstanding balance.
    /// </summary>
    public void ApplyPayment(decimal amount, DateTime nowUtc)
    {
        if (amount <= 0)
        {
            throw new DomainValidationException("Payment amount must be positive.");
        }

        PaidAmount += amount;
        ModifiedUtc = nowUtc;

        if (PaidAmount >= BilledAmount)
        {
            CurrentStatus = ClaimStatus.Completed;
        }
        else
        {
            CurrentStatus = ClaimStatus.PendingPayment;
        }

        _history.Add(ClaimLifecycleEvent.Create(CurrentStage, StepOutcome.Success,
            $"Payment of {amount:C} applied. Total paid: {PaidAmount:C} of {BilledAmount:C}.", null, nowUtc));
    }

    /// <summary>
    /// Attaches a monitoring alert to the claim.
    /// </summary>
    public void RaiseAlert(ClaimAlert alert)
    {
        ArgumentNullException.ThrowIfNull(alert);
        _alerts.Add(alert);
    }
}
