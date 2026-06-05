using BillingService.Application.Abstractions.Messaging;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class OutboxMessageTests
{
    [Fact]
    public void OutboxMessage_HasCorrectInitialState()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "billing.operation.completed",
            Payload = "{}",
            OccurredOnUtc = DateTimeOffset.UtcNow,
            AttemptCount = 0
        };

        Assert.Null(message.ProcessedOnUtc);
        Assert.Null(message.ProcessingError);
        Assert.Equal(0, message.AttemptCount);
    }

    [Fact]
    public void OutboxMessage_CanRecordProcessing()
    {
        var processedAt = DateTimeOffset.UtcNow;

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "billing.operation.completed",
            Payload = "{}",
            OccurredOnUtc = DateTimeOffset.UtcNow,
            AttemptCount = 0,
            ProcessedOnUtc = processedAt
        };

        Assert.Equal(processedAt, message.ProcessedOnUtc);
    }

    [Fact]
    public void OutboxMessage_CanRecordFailure()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "billing.operation.completed",
            Payload = "{}",
            OccurredOnUtc = DateTimeOffset.UtcNow,
            AttemptCount = 1,
            ProcessingError = "Service Bus connection timed out."
        };

        Assert.Equal("Service Bus connection timed out.", message.ProcessingError);
        Assert.Equal(1, message.AttemptCount);
        Assert.Null(message.ProcessedOnUtc);
    }
}
