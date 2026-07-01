namespace ClaimLifecycleMonitoring.Domain.Enums;

/// <summary>
/// Enumerates the failure categories the monitoring engine can automatically detect.
/// </summary>
public enum FailureCategory
{
    /// <summary>No failure detected.</summary>
    None = 0,
    /// <summary>The claim stopped progressing within its SLA window.</summary>
    Stuck = 1,
    /// <summary>The claim exceeded a maximum lifecycle age.</summary>
    Timeout = 2,
    /// <summary>Claim-level validation failed.</summary>
    ValidationFailure = 3,
    /// <summary>Payer eligibility check failed.</summary>
    EligibilityFailure = 4,
    /// <summary>Authorization was denied or could not be obtained.</summary>
    AuthorizationFailure = 5,
    /// <summary>The 837 EDI file could not be generated.</summary>
    EdiGenerationFailure = 6,
    /// <summary>The 837 EDI file could not be submitted.</summary>
    EdiSubmissionFailure = 7,
    /// <summary>A 999 acknowledgement was not received in time.</summary>
    Missing999 = 8,
    /// <summary>A 277CA acknowledgement was not received in time.</summary>
    Missing277CA = 9,
    /// <summary>An 835 remittance advice was not received in time.</summary>
    Missing835 = 10,
    /// <summary>Payment posting failed on the general ledger side.</summary>
    PaymentPostingFailure = 11,
    /// <summary>A patient invoice could not be generated.</summary>
    InvoiceGenerationFailure = 12,
    /// <summary>The claim is a duplicate of another claim.</summary>
    DuplicateClaim = 13,
    /// <summary>The claim was cancelled by an operator or upstream system.</summary>
    CancelledClaim = 14,
    /// <summary>The claim was reprocessed after a failure.</summary>
    ReprocessedClaim = 15,
    /// <summary>The claim breached the configured SLA.</summary>
    SlaBreach = 16
}
