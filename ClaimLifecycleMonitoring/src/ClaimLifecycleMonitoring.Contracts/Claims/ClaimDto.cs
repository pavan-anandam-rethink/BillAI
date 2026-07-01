namespace ClaimLifecycleMonitoring.Contracts.Claims;

/// <summary>
/// Read-only projection of a claim used by the API and dashboard.
/// </summary>
public sealed class ClaimDto
{
    /// <summary>Persistent identifier.</summary>
    public long Id { get; init; }

    /// <summary>Claim number.</summary>
    public string ClaimNumber { get; init; } = string.Empty;

    /// <summary>Customer code.</summary>
    public string CustomerCode { get; init; } = string.Empty;

    /// <summary>Customer name.</summary>
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>Account code.</summary>
    public string AccountCode { get; init; } = string.Empty;

    /// <summary>Account name.</summary>
    public string AccountName { get; init; } = string.Empty;

    /// <summary>Patient identifier.</summary>
    public string PatientId { get; init; } = string.Empty;

    /// <summary>Patient name.</summary>
    public string PatientName { get; init; } = string.Empty;

    /// <summary>Provider identifier.</summary>
    public string ProviderId { get; init; } = string.Empty;

    /// <summary>Provider name.</summary>
    public string ProviderName { get; init; } = string.Empty;

    /// <summary>Current lifecycle stage (numeric enum value).</summary>
    public int CurrentStage { get; init; }

    /// <summary>Current lifecycle stage name.</summary>
    public string CurrentStageName { get; init; } = string.Empty;

    /// <summary>Current status (numeric enum value).</summary>
    public int CurrentStatus { get; init; }

    /// <summary>Current status name.</summary>
    public string CurrentStatusName { get; init; } = string.Empty;

    /// <summary>Last successful stage (numeric enum value).</summary>
    public int? LastSuccessfulStage { get; init; }

    /// <summary>Last successful stage name.</summary>
    public string? LastSuccessfulStageName { get; init; }

    /// <summary>Next expected stage (numeric enum value).</summary>
    public int? NextExpectedStage { get; init; }

    /// <summary>Next expected stage name.</summary>
    public string? NextExpectedStageName { get; init; }

    /// <summary>Age of the claim in hours.</summary>
    public double ClaimAgeHours { get; init; }

    /// <summary>Submission timestamp (UTC).</summary>
    public DateTime? SubmissionDateUtc { get; init; }

    /// <summary>Last modification timestamp (UTC).</summary>
    public DateTime? LastUpdatedUtc { get; init; }

    /// <summary>Whether the claim currently has a failure.</summary>
    public bool HasFailure { get; init; }

    /// <summary>Failure category name.</summary>
    public string FailureCategoryName { get; init; } = string.Empty;

    /// <summary>Exception message associated with the last failure.</summary>
    public string? ExceptionMessage { get; init; }

    /// <summary>Whether the claim can be retried.</summary>
    public bool RetryAvailable { get; init; }

    /// <summary>Whether the claim requires manual action.</summary>
    public bool ManualInterventionRequired { get; init; }

    /// <summary>Total billed amount.</summary>
    public decimal BilledAmount { get; init; }

    /// <summary>Total amount paid so far.</summary>
    public decimal PaidAmount { get; init; }
}
