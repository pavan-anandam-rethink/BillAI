using System.Text.Json;
using Azure.Messaging.ServiceBus;
using ClearingHouse.SharedKernel.Messaging;
using ClearingHouse.SharedKernel.Observability;
using Microsoft.Extensions.Logging;

namespace SftpIngestion.Infrastructure.Messaging;

public class ServiceBusEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ICorrelationContext _correlationContext;
    private readonly ILogger<ServiceBusEventPublisher> _logger;

    public ServiceBusEventPublisher(ServiceBusClient client, ICorrelationContext correlationContext, ILogger<ServiceBusEventPublisher> logger)
    {
        _client = client;
        _correlationContext = correlationContext;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IIntegrationEvent
    {
        await PublishAsync(@event, typeof(T).Name, cancellationToken);
    }

    public async Task PublishAsync<T>(T @event, string topicName, CancellationToken cancellationToken = default) where T : IIntegrationEvent
    {
        await using var sender = _client.CreateSender(topicName);

        var message = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(@event))
        {
            ContentType = "application/json",
            MessageId = @event.EventId.ToString(),
            CorrelationId = _correlationContext.CorrelationId,
            Subject = @event.EventType,
            ApplicationProperties =
            {
                ["EventType"] = typeof(T).FullName ?? typeof(T).Name,
                ["CorrelationId"] = _correlationContext.CorrelationId,
                ["PublishedAt"] = DateTime.UtcNow.ToString("O")
            }
        };

        await sender.SendMessageAsync(message, cancellationToken);
        _logger.LogInformation("Published event {EventType} to {Topic} with correlation {CorrelationId}",
            typeof(T).Name, topicName, _correlationContext.CorrelationId);
    }

    public async ValueTask DisposeAsync() => await _client.DisposeAsync();
}
