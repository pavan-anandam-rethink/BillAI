using ClearingHouse.SharedKernel.Domain;

namespace StediIntegration.Domain.Entities;

public class StediWebhookEvent : AggregateRoot
{
    public string WebhookId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public bool IsProcessed { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private StediWebhookEvent() { }

    public static StediWebhookEvent Create(string webhookId, string eventType, string payload)
    {
        return new StediWebhookEvent
        {
            WebhookId = webhookId,
            EventType = eventType,
            Payload = payload,
            ReceivedAt = DateTime.UtcNow
        };
    }

    public void MarkAsProcessed(string? correlationId = null)
    {
        IsProcessed = true;
        ProcessedAt = DateTime.UtcNow;
        CorrelationId = correlationId;
        IncrementVersion();
    }
}
