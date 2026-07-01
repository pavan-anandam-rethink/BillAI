namespace ClaimLifecycleMonitoring.Application.Configuration;

/// <summary>
/// Configuration options for the claim lifecycle monitoring engine.
/// </summary>
public sealed class MonitoringOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Monitoring";

    /// <summary>Interval at which the background monitor scans for stuck claims (seconds).</summary>
    public int ScanIntervalSeconds { get; set; } = 60;

    /// <summary>Maximum number of claims processed per scan iteration.</summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>Default SLA (hours) applied when no stage-specific configuration exists.</summary>
    public int DefaultStageSlaHours { get; set; } = 24;

    /// <summary>Maximum hours a claim may live before it is considered timed out.</summary>
    public int GlobalClaimTimeoutHours { get; set; } = 720;

    /// <summary>Hours after 837 submission a 999 acknowledgement is expected.</summary>
    public int Expected999Hours { get; set; } = 24;

    /// <summary>Hours after 999 receipt a 277CA acknowledgement is expected.</summary>
    public int Expected277CaHours { get; set; } = 72;

    /// <summary>Hours after 277CA a 835 remittance is expected.</summary>
    public int Expected835Hours { get; set; } = 720;
}
