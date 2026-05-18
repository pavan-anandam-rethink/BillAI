using Azure.Messaging.ServiceBus;
using BillingService.Application.Abstractions.Messaging;
using BillingService.Contracts.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BillingService.Infrastructure.Messaging;

public sealed class AzureServiceBusEventBus(
    ServiceBusClient serviceBusClient,
    IOptions<ServiceBusOptions> options,
    ILogger<AzureServiceBusEventBus> logger)
    : IEventBus
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        await using var sender = serviceBusClient.CreateSender(options.Value.TopicName);
        var message = new ServiceBusMessage(JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), SerializerOptions))
        {
            MessageId = integrationEvent.EventId.ToString("N"),
            CorrelationId = integrationEvent.CorrelationId,
            Subject = integrationEvent.EventType,
            ContentType = "application/json"
        };

        message.ApplicationProperties["aggregateType"] = integrationEvent.AggregateType;
        message.ApplicationProperties["aggregateId"] = integrationEvent.AggregateId;
        message.ApplicationProperties["schemaVersion"] = integrationEvent.SchemaVersion;

        await sender.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Published BillingService integration event {EventType} with id {EventId}",
            integrationEvent.EventType,
            integrationEvent.EventId);
    }
}
