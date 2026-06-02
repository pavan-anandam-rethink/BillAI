using BillingService.Contracts.Events;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class IntegrationEventTests
{
    [Fact]
    public void BillingOperationCompletedEvent_HasExpectedEventType()
    {
        var evt = new BillingOperationCompletedEvent(
            operationName: "SaveClaim",
            aggregateType: "Claim",
            aggregateId: "42",
            accountInfoId: 100,
            userId: 7,
            correlationId: "corr-abc");

        Assert.Equal("billing.operation.completed", evt.EventType);
        Assert.Equal("SaveClaim", evt.OperationName);
        Assert.Equal("Claim", evt.AggregateType);
        Assert.Equal("42", evt.AggregateId);
        Assert.Equal(100, evt.AccountInfoId);
        Assert.Equal(7, evt.UserId);
        Assert.Equal("corr-abc", evt.CorrelationId);
    }

    [Fact]
    public void IntegrationEvent_HasStableEventId()
    {
        var evt1 = new BillingOperationCompletedEvent("op", "Claim", "1", 1, 1);
        var evt2 = new BillingOperationCompletedEvent("op", "Claim", "1", 1, 1);

        Assert.NotEqual(evt1.EventId, evt2.EventId);
    }

    [Fact]
    public void IntegrationEvent_HasSchemaVersion()
    {
        var evt = new BillingOperationCompletedEvent("op", "Claim", "1", 1, 1);

        Assert.True(evt.SchemaVersion > 0);
    }
}
