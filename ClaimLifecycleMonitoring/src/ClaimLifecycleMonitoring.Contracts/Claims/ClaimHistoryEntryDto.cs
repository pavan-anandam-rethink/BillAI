namespace ClaimLifecycleMonitoring.Contracts.Claims;

/// <summary>
/// A single entry in a claim's lifecycle history.
/// </summary>
public sealed class ClaimHistoryEntryDto
{
    /// <summary>Persistent identifier.</summary>
    public long Id { get; init; }

    /// <summary>Stage at which the event occurred (numeric enum value).</summary>
    public int Stage { get; init; }

    /// <summary>Stage name.</summary>
    public string StageName { get; init; } = string.Empty;

    /// <summary>Outcome (numeric enum value).</summary>
    public int Outcome { get; init; }

    /// <summary>Outcome name.</summary>
    public string OutcomeName { get; init; } = string.Empty;

    /// <summary>Message describing the event.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Optional exception detail.</summary>
    public string? ExceptionDetail { get; init; }

    /// <summary>UTC timestamp of the event.</summary>
    public DateTime OccurredUtc { get; init; }
}
