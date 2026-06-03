using BillingService.Application.Abstractions.Messaging;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class OutboxMessageTests
{
    [Fact]
    public void OutboxMessage_Id_IsGuid()
    {
        var msg = new OutboxMessage { Id = Guid.NewGuid(), EventType = "Test", Payload = "{}" };
        Assert.IsType<Guid>(msg.Id);
    }

    [Fact]
    public void OutboxMessage_CorrelationId_IsNullable()
    {
        var msg = new OutboxMessage { Id = Guid.NewGuid(), EventType = "Test", Payload = "{}", CorrelationId = null };
        Assert.Null(msg.CorrelationId);
    }

    [Fact]
    public void OutboxMessage_AttemptCount_DefaultsToZero()
    {
        var msg = new OutboxMessage { Id = Guid.NewGuid(), EventType = "Test", Payload = "{}" };
        Assert.Equal(0, msg.AttemptCount);
    }

    [Fact]
    public void OutboxMessage_ProcessedOnUtc_IsNullable()
    {
        var msg = new OutboxMessage { Id = Guid.NewGuid(), EventType = "Test", Payload = "{}", ProcessedOnUtc = null };
        Assert.Null(msg.ProcessedOnUtc);
    }
}
