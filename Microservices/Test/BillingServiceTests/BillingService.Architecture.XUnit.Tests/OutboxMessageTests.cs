using BillingService.Application.Abstractions.Messaging;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class OutboxMessageTests
{
    [Fact]
    public void OutboxMessage_DefaultValues_AreCorrect()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "billing.operation.completed",
            Payload = "{}",
            OccurredOnUtc = DateTimeOffset.UtcNow
        };

        Assert.Equal(0, message.AttemptCount);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Null(message.ProcessingError);
        Assert.Null(message.CorrelationId);
    }

    [Fact]
    public void OutboxMessage_ProcessedOnUtc_IsMutable()
    {
        var now = DateTimeOffset.UtcNow;
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "billing.operation.completed",
            Payload = "{}",
            OccurredOnUtc = now
        };

        message.ProcessedOnUtc = now;

        Assert.Equal(now, message.ProcessedOnUtc);
    }

    [Fact]
    public void OutboxMessage_AttemptCount_IsMutable()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "billing.operation.completed",
            Payload = "{}",
            OccurredOnUtc = DateTimeOffset.UtcNow
        };

        message.AttemptCount = 3;

        Assert.Equal(3, message.AttemptCount);
    }

    [Fact]
    public void ModernizationFeatureFlags_EnableBlobFirstArchitecture_DefaultsToFalse()
    {
        var flags = new BillingService.Application.Common.Configuration.ModernizationFeatureFlags();

        Assert.False(flags.EnableBlobFirstArchitecture);
    }

    [Fact]
    public void ModernizationFeatureFlags_EnableOutboxPersistence_DefaultsToFalse()
    {
        var flags = new BillingService.Application.Common.Configuration.ModernizationFeatureFlags();

        Assert.False(flags.EnableOutboxPersistence);
    }

    [Fact]
    public void ModernizationFeatureFlags_EnableOpenTelemetry_DefaultsToTrue()
    {
        var flags = new BillingService.Application.Common.Configuration.ModernizationFeatureFlags();

        // OpenTelemetry is safe to enable by default as it has no breaking effect on behavior.
        Assert.True(flags.EnableOpenTelemetry);
    }
}
