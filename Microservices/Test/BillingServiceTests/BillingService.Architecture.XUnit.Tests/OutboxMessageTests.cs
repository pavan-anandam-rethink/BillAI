using BillingService.Application.Abstractions.Messaging;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class OutboxMessageTests
{
    [Fact]
    public void OutboxMessage_DefaultsAreCorrect()
    {
        var message = new OutboxMessage();

        Assert.Equal(Guid.Empty, message.Id);
        Assert.Equal(string.Empty, message.EventType);
        Assert.Equal(string.Empty, message.Payload);
        Assert.Null(message.CorrelationId);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Null(message.ProcessingError);
        Assert.Equal(0, message.AttemptCount);
    }

    [Fact]
    public void OutboxMessage_CanSetAllProperties()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var message = new OutboxMessage
        {
            Id = id,
            EventType = "billing.operation.completed",
            Payload = """{"key":"value"}""",
            CorrelationId = "corr-123",
            OccurredOnUtc = now,
            ProcessedOnUtc = now.AddSeconds(5),
            ProcessingError = null,
            AttemptCount = 2
        };

        Assert.Equal(id, message.Id);
        Assert.Equal("billing.operation.completed", message.EventType);
        Assert.Equal("""{"key":"value"}""", message.Payload);
        Assert.Equal("corr-123", message.CorrelationId);
        Assert.Equal(now, message.OccurredOnUtc);
        Assert.Equal(now.AddSeconds(5), message.ProcessedOnUtc);
        Assert.Equal(2, message.AttemptCount);
    }
}
