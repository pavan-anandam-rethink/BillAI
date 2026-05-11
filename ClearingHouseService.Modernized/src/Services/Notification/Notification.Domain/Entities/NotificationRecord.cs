using ClearingHouse.SharedKernel.Domain;

namespace Notification.Domain.Entities;

public class NotificationRecord : AggregateRoot
{
    public string Type { get; private set; } = string.Empty;
    public string Severity { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public bool IsSent { get; private set; }
    public DateTime? SentAt { get; private set; }
    public string? Channel { get; private set; }
    public IDictionary<string, string> Properties { get; private set; } = new Dictionary<string, string>();

    private NotificationRecord() { }

    public static NotificationRecord Create(string type, string severity, string title, string message, string correlationId)
    {
        return new NotificationRecord
        {
            Type = type,
            Severity = severity,
            Title = title,
            Message = message,
            CorrelationId = correlationId
        };
    }

    public void MarkAsSent(string channel)
    {
        IsSent = true;
        SentAt = DateTime.UtcNow;
        Channel = channel;
        IncrementVersion();
    }

    public void MarkAsRead()
    {
        IsRead = true;
        IncrementVersion();
    }
}
