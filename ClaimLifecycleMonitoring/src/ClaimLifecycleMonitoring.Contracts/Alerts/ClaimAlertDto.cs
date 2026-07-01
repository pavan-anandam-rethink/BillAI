namespace ClaimLifecycleMonitoring.Contracts.Alerts;

/// <summary>
/// Read-only projection of a claim alert.
/// </summary>
public sealed class ClaimAlertDto
{
    /// <summary>Persistent identifier.</summary>
    public long Id { get; init; }

    /// <summary>Owning claim identifier.</summary>
    public long ClaimId { get; init; }

    /// <summary>Owning claim number.</summary>
    public string ClaimNumber { get; init; } = string.Empty;

    /// <summary>Failure category (numeric enum value).</summary>
    public int Category { get; init; }

    /// <summary>Failure category name.</summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>Alert severity (numeric enum value).</summary>
    public int Severity { get; init; }

    /// <summary>Alert severity name.</summary>
    public string SeverityName { get; init; } = string.Empty;

    /// <summary>Alert message.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Stage the claim was in when the alert was raised (numeric enum value).</summary>
    public int StageAtAlert { get; init; }

    /// <summary>Stage name at alert time.</summary>
    public string StageAtAlertName { get; init; } = string.Empty;

    /// <summary>Alert timestamp (UTC).</summary>
    public DateTime RaisedUtc { get; init; }

    /// <summary>Whether the alert has been acknowledged.</summary>
    public bool Acknowledged { get; init; }

    /// <summary>Acknowledged timestamp (UTC).</summary>
    public DateTime? AcknowledgedUtc { get; init; }

    /// <summary>User who acknowledged the alert.</summary>
    public string? AcknowledgedBy { get; init; }
}
