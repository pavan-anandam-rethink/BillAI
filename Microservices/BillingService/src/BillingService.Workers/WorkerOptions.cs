namespace BillingService.Workers;

public sealed class WorkerOptions
{
    public int OutboxBatchSize { get; set; } = 50;
    public int PollIntervalSeconds { get; set; } = 10;

    public void Normalize()
    {
        if (OutboxBatchSize <= 0)
        {
            OutboxBatchSize = 50;
        }

        if (PollIntervalSeconds <= 0)
        {
            PollIntervalSeconds = 10;
        }
    }
}
