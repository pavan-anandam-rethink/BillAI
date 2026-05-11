namespace ClearingHouse.Contracts.Events;

public record NotificationEvent
{
    public Guid NotificationId { get; init; } = Guid.NewGuid();
    public string Type { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public IDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
