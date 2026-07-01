namespace ClaimLifecycleMonitoring.Contracts.Claims;

/// <summary>
/// Request payload for advancing a claim to a new lifecycle stage.
/// </summary>
public sealed class AdvanceClaimStageRequest
{
    /// <summary>Target stage the claim should advance to (numeric <see cref="Enums"/> value).</summary>
    public int TargetStage { get; set; }

    /// <summary>Optional message describing the reason for the stage transition.</summary>
    public string? Message { get; set; }
}

/// <summary>
/// Request payload for recording a failure against a claim.
/// </summary>
public sealed class RecordFailureRequest
{
    /// <summary>Failure category (numeric enum value).</summary>
    public int FailureCategory { get; set; }

    /// <summary>Exception message.</summary>
    public string ExceptionMessage { get; set; } = string.Empty;

    /// <summary>Whether the failure is retriable.</summary>
    public bool RetryAvailable { get; set; }

    /// <summary>Whether the failure requires manual intervention.</summary>
    public bool ManualInterventionRequired { get; set; }
}

/// <summary>
/// Request payload for cancelling a claim.
/// </summary>
public sealed class CancelClaimRequest
{
    /// <summary>Reason the claim is being cancelled.</summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request payload for applying a payment to a claim.
/// </summary>
public sealed class ApplyPaymentRequest
{
    /// <summary>The payment amount to apply.</summary>
    public decimal Amount { get; set; }
}

/// <summary>
/// Request payload for retrying a failed claim.
/// </summary>
public sealed class RetryClaimRequest
{
    /// <summary>Optional message describing the retry.</summary>
    public string? Message { get; set; }
}

/// <summary>
/// Request payload for acknowledging an alert.
/// </summary>
public sealed class AcknowledgeAlertRequest
{
    /// <summary>User identifier acknowledging the alert.</summary>
    public string UserId { get; set; } = string.Empty;
}
