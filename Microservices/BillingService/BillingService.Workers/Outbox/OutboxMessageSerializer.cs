using BillingService.Application.Abstractions.Messaging;
using BillingService.Contracts.Events;
using System.Text.Json;

namespace BillingService.Workers.Outbox;

/// <summary>
/// Deserializes an <see cref="OutboxMessage"/> payload back to its concrete
/// <see cref="IntegrationEvent"/> subtype using the EventType discriminator.
/// If the event type is unrecognised, returns null so the worker can mark the message failed
/// rather than crashing the entire batch.
/// </summary>
internal static class OutboxMessageSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    // Registry maps EventType strings to their concrete CLR types. Extend this as new
    // integration events are added to BillingService.Contracts.
    private static readonly IReadOnlyDictionary<string, Type> EventTypeRegistry =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["billing.operation.completed"] = typeof(BillingOperationCompletedEvent)
        };

    public static IntegrationEvent? Deserialize(OutboxMessage message)
    {
        if (!EventTypeRegistry.TryGetValue(message.EventType, out var targetType))
        {
            return null;
        }

        return (IntegrationEvent?)JsonSerializer.Deserialize(message.Payload, targetType, Options);
    }
}
