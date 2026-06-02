namespace BillingService.Workers.Outbox;

public sealed class OutboxPublisherOptions
{
    public const string SectionName = "BillingService:Outbox";

    /// <summary>
    /// Seconds between outbox polling iterations. Default: 30.
    /// </summary>
    public int PollingIntervalSeconds { get; init; } = 30;

    /// <summary>
    /// Maximum number of outbox messages processed per iteration. Default: 50.
    /// </summary>
    public int BatchSize { get; init; } = 50;
}
