namespace ClearingHouse.Contracts.Events;

public record BatchCompletedEvent
{
    public Guid BatchId { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public int TotalFiles { get; init; }
    public int ProcessedFiles { get; init; }
    public int FailedFiles { get; init; }
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
}
