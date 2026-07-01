namespace ClaimLifecycleMonitoring.Application.Abstractions.Time;

/// <summary>
/// Abstraction over the system clock used by application services so that
/// unit tests may deterministically control the flow of time.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Gets the current UTC time.</summary>
    DateTime UtcNow { get; }
}
