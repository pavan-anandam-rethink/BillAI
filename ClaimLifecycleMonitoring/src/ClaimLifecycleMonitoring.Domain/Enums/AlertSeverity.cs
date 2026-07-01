namespace ClaimLifecycleMonitoring.Domain.Enums;

/// <summary>
/// Represents the severity of a monitoring alert raised by the rule engine.
/// </summary>
public enum AlertSeverity
{
    /// <summary>Informational alert; no action required.</summary>
    Info = 1,
    /// <summary>Warning; action may be required soon.</summary>
    Warning = 2,
    /// <summary>Error; immediate action is required.</summary>
    Error = 3,
    /// <summary>Critical; a business SLA has been breached.</summary>
    Critical = 4
}
