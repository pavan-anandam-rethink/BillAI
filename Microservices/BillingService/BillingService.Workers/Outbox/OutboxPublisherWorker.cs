using BillingService.Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BillingService.Workers.Outbox;

/// <summary>
/// Background worker that polls the durable outbox table and publishes pending
/// integration events to the Azure Service Bus event bus.
/// Resolves <see cref="IOutboxPoller"/> and <see cref="IEventBus"/> per batch via a
/// dedicated <see cref="IServiceScope"/> to respect scoped DI lifetimes.
/// </summary>
public sealed class OutboxPublisherWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<OutboxPublisherWorker> logger)
    : BackgroundService
{
    private const int BatchSize = 20;
    private const int MaxAttempts = 5;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);

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
                logger.LogError(ex, "Unexpected error in outbox publisher loop.");
            }

            await Task.Delay(PollingInterval, stoppingToken).ConfigureAwait(false);
        }

        logger.LogInformation("BillingService outbox publisher worker stopped.");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var poller = scope.ServiceProvider.GetRequiredService<IOutboxPoller>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var messages = await poller.FetchPendingAsync(BatchSize, cancellationToken).ConfigureAwait(false);
        if (messages.Count == 0)
        {
            return;
        }

        logger.LogInformation("Processing {Count} outbox message(s).", messages.Count);

        foreach (var message in messages)
        {
            if (message.AttemptCount >= MaxAttempts)
            {
                logger.LogWarning(
                    "Outbox message {MessageId} ({EventType}) exceeded max attempts ({Max}); skipping.",
                    message.Id, message.EventType, MaxAttempts);
                continue;
            }

            try
            {
                var integrationEvent = DeserializeEvent(message);
                if (integrationEvent is null)
                {
                    await poller.RecordFailureAsync(
                        message.Id,
                        $"Unknown or undeserializable event type: {message.EventType}",
                        cancellationToken).ConfigureAwait(false);
                    continue;
                }

                await eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
                await poller.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);

                logger.LogInformation(
                    "Published outbox message {MessageId} ({EventType}).",
                    message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to publish outbox message {MessageId} ({EventType}). Attempt {Attempt}.",
                    message.Id, message.EventType, message.AttemptCount + 1);

                await poller.RecordFailureAsync(message.Id, ex.Message, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private static BillingService.Contracts.Events.IntegrationEvent? DeserializeEvent(OutboxMessage message)
    {
        // Event type to CLR type resolution. Extend this map as new event types are introduced.
        var eventType = System.Type.GetType(message.EventType, throwOnError: false);
        if (eventType is null || !eventType.IsAssignableTo(typeof(BillingService.Contracts.Events.IntegrationEvent)))
        {
            return null;
        }

        return (BillingService.Contracts.Events.IntegrationEvent?)
            System.Text.Json.JsonSerializer.Deserialize(
                message.Payload,
                eventType,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
    }
}
