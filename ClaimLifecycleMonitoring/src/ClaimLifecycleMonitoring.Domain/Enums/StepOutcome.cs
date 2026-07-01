namespace ClaimLifecycleMonitoring.Domain.Enums;

/// <summary>
/// Represents the outcome of an individual step in the claim lifecycle history.
/// </summary>
public enum StepOutcome
{
    /// <summary>The step is currently in progress.</summary>
    InProgress = 1,
    /// <summary>The step completed successfully.</summary>
    Success = 2,
    /// <summary>The step failed with an exception or business error.</summary>
    Failure = 3,
    /// <summary>The step was skipped intentionally.</summary>
    Skipped = 4,
    /// <summary>The step timed out waiting for an external event.</summary>
    Timeout = 5,
    /// <summary>The step was retried after a failure.</summary>
    Retried = 6
}
