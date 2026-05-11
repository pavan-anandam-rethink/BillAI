using System.Text.Json;
using Azure.Messaging.ServiceBus;
using ClearingHouse.SharedKernel.Interfaces;
using Microsoft.Extensions.Logging;

namespace SftpIngestion.Infrastructure.Messaging;

public class ServiceBusEventBus : IEventBus
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusEventBus> _logger;

    public ServiceBusEventBus(ServiceBusClient client, ILogger<ServiceBusEventBus> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        var topicName = typeof(T).Name.ToLowerInvariant();
        await PublishAsync(@event, topicName, cancellationToken);
    }

    public async Task PublishAsync<T>(T @event, string topicOrQueue, CancellationToken cancellationToken = default) where T : class
    {
        await using var sender = _client.CreateSender(topicOrQueue);
        var message = new ServiceBusMessage(JsonSerializer.Serialize(@event))
        {
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
            Subject = typeof(T).Name
        };

        await sender.SendMessageAsync(message, cancellationToken);
        _logger.LogInformation("Published {EventType} to {TopicOrQueue}", typeof(T).Name, topicOrQueue);
    }
}
