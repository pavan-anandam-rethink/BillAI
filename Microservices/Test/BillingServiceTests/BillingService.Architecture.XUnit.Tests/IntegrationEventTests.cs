using BillingService.Contracts.Events;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class IntegrationEventTests
{
    [Fact]
    public void IntegrationEvent_NewInstance_GeneratesUniqueEventId()
    {
        var a = new BillingOperationCompletedEvent("SaveClaim", "claim", "C-1", 1, 1);
        var b = new BillingOperationCompletedEvent("SaveClaim", "claim", "C-2", 1, 1);

        Assert.NotEqual(a.EventId, b.EventId);
    }

    [Fact]
    public void IntegrationEvent_EventType_IsExpectedDiscriminator()
    {
        var ev = new BillingOperationCompletedEvent("SaveClaim", "claim", "C-1", 1, 1);

        Assert.Equal("billing.operation.completed", ev.EventType);
    }

    [Fact]
    public void IntegrationEvent_SchemaVersion_DefaultsToOne()
    {
        var ev = new BillingOperationCompletedEvent("PostPayment", "payment", "P-1", 1, 1);

        Assert.Equal(1, ev.SchemaVersion);
    }

    [Fact]
    public void IntegrationEvent_OccurredOnUtc_IsRecentUtc()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var ev = new BillingOperationCompletedEvent("PostPayment", "payment", "P-1", 1, 1);
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        Assert.True(ev.OccurredOnUtc >= before && ev.OccurredOnUtc <= after);
    }
}
