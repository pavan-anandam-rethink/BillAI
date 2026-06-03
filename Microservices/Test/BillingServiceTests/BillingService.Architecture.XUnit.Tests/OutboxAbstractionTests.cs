using BillingService.Application.Abstractions.Messaging;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class OutboxAbstractionTests
{
    [Fact]
    public void IOutboxPoller_IsInterface()
    {
        Assert.True(typeof(IOutboxPoller).IsInterface);
    }

    [Fact]
    public void IOutboxPoller_HasFetchPendingAsync()
    {
        var method = typeof(IOutboxPoller).GetMethod(nameof(IOutboxPoller.FetchPendingAsync));

        Assert.NotNull(method);
        // First parameter is batchSize (int)
        var param = method!.GetParameters()[0];
        Assert.Equal(typeof(int), param.ParameterType);
    }

    [Fact]
    public void IOutboxPoller_HasMarkProcessedAsync()
    {
        var method = typeof(IOutboxPoller).GetMethod(nameof(IOutboxPoller.MarkProcessedAsync));

        Assert.NotNull(method);
        // First parameter is messageId (Guid)
        var param = method!.GetParameters()[0];
        Assert.Equal(typeof(Guid), param.ParameterType);
    }

    [Fact]
    public void IOutboxPoller_HasMarkFailedAsync()
    {
        var method = typeof(IOutboxPoller).GetMethod(nameof(IOutboxPoller.MarkFailedAsync));

        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Equal(typeof(Guid), parameters[0].ParameterType);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
    }

    [Fact]
    public void OutboxMessage_HasIdAndEventTypeAndPayload()
    {
        var idProp = typeof(OutboxMessage).GetProperty(nameof(OutboxMessage.Id));
        var eventTypeProp = typeof(OutboxMessage).GetProperty(nameof(OutboxMessage.EventType));
        var payloadProp = typeof(OutboxMessage).GetProperty(nameof(OutboxMessage.Payload));

        Assert.NotNull(idProp);
        Assert.NotNull(eventTypeProp);
        Assert.NotNull(payloadProp);

        Assert.Equal(typeof(Guid), idProp!.PropertyType);
        Assert.Equal(typeof(string), eventTypeProp!.PropertyType);
        Assert.Equal(typeof(string), payloadProp!.PropertyType);
    }

    [Fact]
    public void OutboxMessage_HasAttemptCountAndProcessedOnUtc()
    {
        var attemptCount = typeof(OutboxMessage).GetProperty(nameof(OutboxMessage.AttemptCount));
        var processedOnUtc = typeof(OutboxMessage).GetProperty(nameof(OutboxMessage.ProcessedOnUtc));

        Assert.NotNull(attemptCount);
        Assert.NotNull(processedOnUtc);

        Assert.Equal(typeof(int), attemptCount!.PropertyType);

        // ProcessedOnUtc is nullable – it is null until the message is delivered
        var nullabilityCtx = new System.Reflection.NullabilityInfoContext();
        var info = nullabilityCtx.Create(processedOnUtc!);
        Assert.Equal(System.Reflection.NullabilityState.Nullable, info.WriteState);
    }

    [Fact]
    public void IOutboxWriter_IsInterface()
    {
        Assert.True(typeof(IOutboxWriter).IsInterface);
    }

    [Fact]
    public void IOutboxWriter_HasAddAsync()
    {
        var method = typeof(IOutboxWriter).GetMethod(nameof(IOutboxWriter.AddAsync));
        Assert.NotNull(method);
    }
}
