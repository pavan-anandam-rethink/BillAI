using BillingService.Application.Abstractions.Correlation;
using BillingService.Application.Abstractions.Messaging;
using BillingService.Contracts.Events;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class CorrelationAndOutboxTests
{
    [Fact]
    public void ICorrelationIdProvider_DefinesGetCorrelationId()
    {
        var method = typeof(ICorrelationIdProvider).GetMethod(nameof(ICorrelationIdProvider.GetCorrelationId));

        Assert.NotNull(method);
        Assert.Equal(typeof(string), method!.ReturnType);
    }

    [Fact]
    public void IOutboxWriter_DefinesAddAsync()
    {
        var method = typeof(IOutboxWriter).GetMethod(nameof(IOutboxWriter.AddAsync));

        Assert.NotNull(method);
        // Parameter must accept IntegrationEvent
        var parameters = method!.GetParameters();
        Assert.True(parameters.Length >= 1);
        Assert.True(
            typeof(IntegrationEvent).IsAssignableFrom(parameters[0].ParameterType),
            "IOutboxWriter.AddAsync must accept an IntegrationEvent.");
    }

    [Fact]
    public void IOutboxPoller_DefinesReadPendingAndMarkProcessed()
    {
        var methods = typeof(IOutboxPoller).GetMethods();
        var methodNames = methods.Select(m => m.Name).ToHashSet();

        Assert.Contains(nameof(IOutboxPoller.ReadPendingAsync), methodNames);
        Assert.Contains(nameof(IOutboxPoller.MarkProcessedAsync), methodNames);
    }

    [Fact]
    public void PendingOutboxMessage_IsImmutableRecord()
    {
        var type = typeof(PendingOutboxMessage);

        Assert.True(type.IsValueType || IsRecord(type),
            "PendingOutboxMessage should be a positional record to enforce immutability.");
    }

    [Fact]
    public void IntegrationEvent_CarriesEventId()
    {
        // BillingOperationCompletedEvent must have a stable EventId for idempotent delivery.
        var evt = new BillingOperationCompletedEvent(
            operationName: "SaveClaim",
            aggregateType: "Claim",
            aggregateId: "42",
            accountInfoId: 1,
            userId: 99,
            correlationId: "test-correlation");

        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.Equal("billing.operation.completed", evt.EventType);
        Assert.Equal("Claim", evt.AggregateType);
        Assert.Equal("42", evt.AggregateId);
        Assert.Equal("test-correlation", evt.CorrelationId);
    }

    [Fact]
    public void IntegrationEvent_SchemaVersionDefaultsToOne()
    {
        var evt = new BillingOperationCompletedEvent(
            operationName: "TestOp",
            aggregateType: "Test",
            aggregateId: "1",
            accountInfoId: 1,
            userId: 1);

        Assert.Equal(1, evt.SchemaVersion);
    }

    private static bool IsRecord(Type type)
    {
        return type.GetMethod("<Clone>$") is not null;
    }
}
