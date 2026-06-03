using BillingService.Application.Abstractions.Messaging;
using BillingService.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BillingService.Workers.Outbox;

public sealed class OutboxPublisherWorker(
    IEventBus eventBus,
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisherWorker> logger)
    : BackgroundService
{
    private const int BatchSize = 20;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(15);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BillingService outbox publisher worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in outbox publisher worker batch loop.");
            }

            await Task.Delay(PollingInterval, stoppingToken).ConfigureAwait(false);
        }

        logger.LogInformation("BillingService outbox publisher worker stopped.");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var poller = scope.ServiceProvider.GetRequiredService<IOutboxPoller>();

        var messages = await poller.FetchPendingAsync(BatchSize, cancellationToken).ConfigureAwait(false);
        if (messages.Count == 0)
        {
            return;
        }

        logger.LogInformation("Processing {MessageCount} outbox messages.", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.EventType);
                if (eventType is null || !typeof(IntegrationEvent).IsAssignableFrom(eventType))
                {
                    logger.LogWarning(
                        "Outbox message {MessageId} has unknown event type {EventType}. Skipping.",
                        message.Id, message.EventType);
                    await poller.RecordFailureAsync(message.Id, $"Unknown event type: {message.EventType}", cancellationToken)
                        .ConfigureAwait(false);
                    continue;
                }

                var integrationEvent = (IntegrationEvent?)JsonSerializer.Deserialize(message.Payload, eventType, SerializerOptions);
                if (integrationEvent is null)
                {
                    await poller.RecordFailureAsync(message.Id, "Payload deserialization returned null.", cancellationToken)
                        .ConfigureAwait(false);
                    continue;
                }

                await eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
                await poller.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);

                logger.LogInformation(
                    "Published outbox message {MessageId} of type {EventType}.",
                    message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish outbox message {MessageId}.", message.Id);
                await poller.RecordFailureAsync(message.Id, ex.Message, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
