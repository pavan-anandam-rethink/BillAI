using ClaimLifecycleMonitoring.Domain.Common;
using ClaimLifecycleMonitoring.Domain.Enums;

namespace ClaimLifecycleMonitoring.Domain.Entities;

/// <summary>
/// Represents a monitoring alert raised against a claim by the rule engine.
/// </summary>
public sealed class ClaimAlert : EntityBase
{
    /// <summary>Foreign key back to the owning claim.</summary>
    public long ClaimId { get; private set; }

    /// <summary>Failure category which triggered the alert.</summary>
    public FailureCategory Category { get; private set; }

    /// <summary>Severity of the alert.</summary>
    public AlertSeverity Severity { get; private set; }

    /// <summary>Human readable description of the alert.</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>Stage the claim was in when the alert was raised.</summary>
    public ClaimStage StageAtAlert { get; private set; }

    /// <summary>UTC timestamp the alert was raised.</summary>
    public DateTime RaisedUtc { get; private set; }

    /// <summary>Whether the alert has been acknowledged by an operator.</summary>
    public bool Acknowledged { get; private set; }

    /// <summary>UTC timestamp the alert was acknowledged.</summary>
    public DateTime? AcknowledgedUtc { get; private set; }

    /// <summary>Operator user id that acknowledged the alert.</summary>
    public string? AcknowledgedBy { get; private set; }

    /// <summary>EF constructor.</summary>
    private ClaimAlert() { }

    /// <summary>
    /// Creates a new <see cref="ClaimAlert"/>.
    /// </summary>
    public static ClaimAlert Create(FailureCategory category, AlertSeverity severity, string message, ClaimStage stage, DateTime nowUtc)
    {
        return new ClaimAlert
        {
            Category = category,
            Severity = severity,
            Message = message,
            StageAtAlert = stage,
            RaisedUtc = nowUtc,
            CreatedUtc = nowUtc,
            Acknowledged = false
        };
    }

    /// <summary>
    /// Acknowledges the alert on behalf of the given user.
    /// </summary>
    public void Acknowledge(string userId, DateTime nowUtc)
    {
        if (Acknowledged)
        {
            return;
        }

        Acknowledged = true;
        AcknowledgedUtc = nowUtc;
        AcknowledgedBy = userId;
        ModifiedUtc = nowUtc;
    }
}
