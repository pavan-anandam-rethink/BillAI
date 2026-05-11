namespace ClearingHouse.Contracts.Events;

public record FileProcessedEvent
{
    public Guid FileId { get; init; }
    public Guid BatchId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public int TotalRecords { get; init; }
    public int SuccessfulRecords { get; init; }
    public int FailedRecords { get; init; }
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
    public TimeSpan ProcessingDuration { get; init; }
}
