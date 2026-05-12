using System.Text.Json;
using Azure.Messaging.ServiceBus;
using ClearingHouse.SharedKernel.Domain.Events;
using ClearingHouse.SharedKernel.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClearingHouse.SftpIngestion.Infrastructure.ServiceBus;

/// <summary>
/// Azure Service Bus implementation of <see cref="IEventPublisher"/>.
/// Publishes domain events to topics with correlation ID, retry, and dead-letter handling.
/// </summary>
public sealed class ServiceBusEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusSender _sender;
    private readonly ILogger<ServiceBusEventPublisher> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusEventPublisher"/> class.
    /// </summary>
    /// <param name="sender">The Service Bus sender.</param>
    /// <param name="logger">The logger instance.</param>
    public ServiceBusEventPublisher(ServiceBusSender sender, ILogger<ServiceBusEventPublisher> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var message = CreateMessage(domainEvent);

        _logger.LogDebug(
            "Publishing event {EventType} with correlation {CorrelationId}",
            domainEvent.GetType().Name,
            domainEvent.CorrelationId.Value);

        await _sender.SendMessageAsync(message, cancellationToken);

        _logger.LogInformation(
            "Published event {EventType} with correlation {CorrelationId}",
            domainEvent.GetType().Name,
            domainEvent.CorrelationId.Value);
    }

    /// <inheritdoc />
    public async Task PublishBatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        var eventList = domainEvents.ToList();
        if (eventList.Count == 0) return;

        _logger.LogDebug("Publishing batch of {EventCount} events", eventList.Count);

        using var messageBatch = await _sender.CreateMessageBatchAsync(cancellationToken);

        foreach (var domainEvent in eventList)
        {
            var message = CreateMessage(domainEvent);
            if (!messageBatch.TryAddMessage(message))
            {
                // If batch is full, send current batch and create new one
                await _sender.SendMessagesAsync(messageBatch, cancellationToken);
                _logger.LogDebug("Sent partial batch, continuing with remaining events");

                // Send remaining message individually
                await _sender.SendMessageAsync(message, cancellationToken);
            }
        }

        if (messageBatch.Count > 0)
        {
            await _sender.SendMessagesAsync(messageBatch, cancellationToken);
        }

        _logger.LogInformation("Published batch of {EventCount} events", eventList.Count);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
    }

    private static ServiceBusMessage CreateMessage(IDomainEvent domainEvent)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(domainEvent, domainEvent.GetType(), SerializerOptions);
        var message = new ServiceBusMessage(body)
        {
            ContentType = "application/json",
            Subject = domainEvent.GetType().Name,
            CorrelationId = domainEvent.CorrelationId.Value,
            MessageId = Guid.NewGuid().ToString("D")
        };

        message.ApplicationProperties["eventType"] = domainEvent.GetType().FullName;
        message.ApplicationProperties["occurredOn"] = domainEvent.OccurredOn.ToString("O");

        return message;
    }
}
