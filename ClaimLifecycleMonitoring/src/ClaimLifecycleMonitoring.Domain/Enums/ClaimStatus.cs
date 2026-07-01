namespace ClaimLifecycleMonitoring.Domain.Enums;

/// <summary>
/// Represents the current processing status of a claim within its lifecycle stage.
/// </summary>
public enum ClaimStatus
{
    /// <summary>The claim is progressing normally.</summary>
    InProgress = 1,

    /// <summary>The claim is waiting on an external event (e.g. payer response).</summary>
    Waiting = 2,

    /// <summary>The claim has completed the full lifecycle successfully.</summary>
    Completed = 3,

    /// <summary>The claim failed and cannot proceed without intervention.</summary>
    Failed = 4,

    /// <summary>The claim was rejected by a validator, clearinghouse or payer.</summary>
    Rejected = 5,

    /// <summary>The claim has stopped progressing beyond an SLA threshold.</summary>
    Stuck = 6,

    /// <summary>The claim requires manual action by an operator.</summary>
    ManualActionRequired = 7,

    /// <summary>The claim has been cancelled and will not be processed further.</summary>
    Cancelled = 8,

    /// <summary>The claim is a duplicate of an existing claim.</summary>
    Duplicate = 9,

    /// <summary>The claim has been reprocessed after a failure.</summary>
    Reprocessed = 10,

    /// <summary>Payment has not yet been received or posted.</summary>
    PendingPayment = 11
}
