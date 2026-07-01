using ClaimLifecycleMonitoring.Domain.Common;
using ClaimLifecycleMonitoring.Domain.Enums;

namespace ClaimLifecycleMonitoring.Domain.Entities;

/// <summary>
/// Immutable audit record of a single event within the claim lifecycle.
/// </summary>
public sealed class ClaimLifecycleEvent : EntityBase
{
    /// <summary>Foreign key back to the owning claim.</summary>
    public long ClaimId { get; private set; }

    /// <summary>Stage the event occurred in.</summary>
    public ClaimStage Stage { get; private set; }

    /// <summary>Outcome of the step.</summary>
    public StepOutcome Outcome { get; private set; }

    /// <summary>Human readable event message.</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>Optional exception details captured when the step failed.</summary>
    public string? ExceptionDetail { get; private set; }

    /// <summary>UTC timestamp the event occurred.</summary>
    public DateTime OccurredUtc { get; private set; }

    /// <summary>EF constructor.</summary>
    private ClaimLifecycleEvent() { }

    /// <summary>
    /// Creates a new immutable <see cref="ClaimLifecycleEvent"/>.
    /// </summary>
    public static ClaimLifecycleEvent Create(ClaimStage stage, StepOutcome outcome, string message, string? exceptionDetail, DateTime nowUtc)
    {
        return new ClaimLifecycleEvent
        {
            Stage = stage,
            Outcome = outcome,
            Message = string.IsNullOrWhiteSpace(message) ? stage.ToString() : message,
            ExceptionDetail = exceptionDetail,
            OccurredUtc = nowUtc,
            CreatedUtc = nowUtc
        };
    }
}
