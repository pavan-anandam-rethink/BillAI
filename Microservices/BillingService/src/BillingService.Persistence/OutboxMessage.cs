namespace BillingService.Persistence;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public int AccountInfoId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset? DispatchedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}
