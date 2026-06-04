using BillingService.Application.Abstractions.Messaging;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class OutboxMessageTests
{
    [Fact]
    public void OutboxMessage_DefaultsAttemptCountToZero()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "BillingOperationCompleted",
            Payload = "{}",
            OccurredOnUtc = DateTimeOffset.UtcNow
        };

        Assert.Equal(0, message.AttemptCount);
    }

    [Fact]
    public void OutboxMessage_AllowsNullCorrelationId()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "BillingOperationCompleted",
            Payload = "{}",
            OccurredOnUtc = DateTimeOffset.UtcNow,
            CorrelationId = null
        };

        Assert.Null(message.CorrelationId);
    }

    [Fact]
    public void OutboxMessage_CanSetProcessedOnUtc()
    {
        var processedAt = DateTimeOffset.UtcNow;
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "BillingOperationCompleted",
            Payload = "{}",
            OccurredOnUtc = DateTimeOffset.UtcNow.AddSeconds(-5),
            ProcessedOnUtc = processedAt
        };

        Assert.Equal(processedAt, message.ProcessedOnUtc);
    }

    [Fact]
    public void OutboxMessage_CanRecordProcessingError()
    {
        const string error = "Connection timeout.";
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "BillingOperationCompleted",
            Payload = "{}",
            OccurredOnUtc = DateTimeOffset.UtcNow,
            ProcessingError = error,
            AttemptCount = 1
        };

        Assert.Equal(error, message.ProcessingError);
        Assert.Equal(1, message.AttemptCount);
    }
}
