using BillingService.Application.Abstractions.Messaging;
using BillingService.Contracts.Claims;
using BillingService.Contracts.Metadata;

namespace BillingService.Architecture.XUnit.Tests;

/// <summary>
/// Contract-level tests for the outbox message model and correlation id conventions.
/// These tests act as living documentation: they fail fast if a refactor accidentally
/// changes the contract shape that the SqlOutboxReader/Writer and worker depend on.
/// </summary>
public sealed class OutboxAndCorrelationContractTests
{
    [Fact]
    public void OutboxMessage_HasRequiredProperties()
    {
        var type = typeof(OutboxMessage);

        Assert.NotNull(type.GetProperty(nameof(OutboxMessage.Id)));
        Assert.NotNull(type.GetProperty(nameof(OutboxMessage.EventType)));
        Assert.NotNull(type.GetProperty(nameof(OutboxMessage.Payload)));
        Assert.NotNull(type.GetProperty(nameof(OutboxMessage.CorrelationId)));
        Assert.NotNull(type.GetProperty(nameof(OutboxMessage.OccurredOnUtc)));
        Assert.NotNull(type.GetProperty(nameof(OutboxMessage.ProcessedOnUtc)));
        Assert.NotNull(type.GetProperty(nameof(OutboxMessage.ProcessingError)));
        Assert.NotNull(type.GetProperty(nameof(OutboxMessage.AttemptCount)));
    }

    [Fact]
    public void OutboxMessage_IdIsGuid()
    {
        var property = typeof(OutboxMessage).GetProperty(nameof(OutboxMessage.Id));
        Assert.Equal(typeof(Guid), property!.PropertyType);
    }

    [Fact]
    public void OutboxMessage_OccurredOnUtcIsDateTimeOffset()
    {
        var property = typeof(OutboxMessage).GetProperty(nameof(OutboxMessage.OccurredOnUtc));
        Assert.Equal(typeof(DateTimeOffset), property!.PropertyType);
    }

    [Fact]
    public void BillingApiContractInventory_DefinesHealthRoute()
    {
        Assert.Equal("/api/health", BillingApiContractInventory.HealthRoute);
    }

    [Fact]
    public void BillingApiContractInventory_HasCompatibilityCriticalControllers()
    {
        var controllers = BillingApiContractInventory.CompatibilityCriticalControllers;
        Assert.NotEmpty(controllers);
        Assert.Contains("Claim", controllers);
        Assert.Contains("PaymentPosting", controllers);
        Assert.Contains("PatientInvoice", controllers);
    }

    [Fact]
    public void ClaimSummaryDto_HasClaimIdentifierProperty()
    {
        var property = typeof(ClaimSummaryDto).GetProperty(nameof(ClaimSummaryDto.ClaimIdentifier));
        Assert.NotNull(property);
        Assert.Equal(typeof(string), property!.PropertyType);
    }

    [Fact]
    public void ClaimSummaryDto_IsImmutableRecord()
    {
        // All properties must be get-init (init-only setter) — immutability check.
        var properties = typeof(ClaimSummaryDto).GetProperties();
        foreach (var prop in properties)
        {
            // init-only setters expose an IsInitOnly custom modifier; if no setter
            // is present at all that's also acceptable for immutability.
            var setter = prop.GetSetMethod(nonPublic: true);
            if (setter is not null)
            {
                Assert.True(
                    setter.ReturnParameter
                          .GetRequiredCustomModifiers()
                          .Any(m => m.FullName?.Contains("IsExternalInit") == true),
                    $"Property {prop.Name} on ClaimSummaryDto should be init-only.");
            }
        }
    }
}
