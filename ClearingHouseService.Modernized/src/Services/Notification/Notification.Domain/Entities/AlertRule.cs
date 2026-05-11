using ClearingHouse.SharedKernel.Domain;

namespace Notification.Domain.Entities;

public class AlertRule : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string Severity { get; private set; } = string.Empty;
    public string Channel { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public string? Condition { get; private set; }
    public string? RecipientGroup { get; private set; }

    private AlertRule() { }

    public static AlertRule Create(string name, string eventType, string severity, string channel, string? condition = null)
    {
        return new AlertRule
        {
            Name = name,
            EventType = eventType,
            Severity = severity,
            Channel = channel,
            IsActive = true,
            Condition = condition
        };
    }

    public void Deactivate()
    {
        IsActive = false;
        IncrementVersion();
    }

    public void Activate()
    {
        IsActive = true;
        IncrementVersion();
    }

    public void SetRecipientGroup(string recipientGroup)
    {
        RecipientGroup = recipientGroup;
        IncrementVersion();
    }
}
