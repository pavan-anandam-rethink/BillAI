namespace ClearingHouse.Contracts.Events;

public record FileFailedEvent
{
    public Guid FileId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public string ErrorCode { get; init; } = string.Empty;
    public int RetryCount { get; init; }
    public bool IsDeadLettered { get; init; }
    public DateTime FailedAt { get; init; } = DateTime.UtcNow;
}
