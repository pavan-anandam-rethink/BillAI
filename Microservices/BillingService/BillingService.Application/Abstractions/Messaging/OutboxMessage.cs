namespace BillingService.Application.Abstractions.Messaging;

public sealed class OutboxMessage
{
    public Guid Id { get; init; }

    public string EventType { get; init; } = string.Empty;

    public string Payload { get; init; } = string.Empty;

    public string? CorrelationId { get; init; }

    public DateTimeOffset OccurredOnUtc { get; init; }

    public DateTimeOffset? ProcessedOnUtc { get; set; }

    public string? ProcessingError { get; set; }

    public int AttemptCount { get; set; }
}
