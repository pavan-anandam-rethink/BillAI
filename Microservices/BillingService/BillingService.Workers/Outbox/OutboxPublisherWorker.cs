using BillingService.Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BillingService.Workers.Outbox;

/// <summary>
/// Background worker that polls the durable BillingOutbox table for unprocessed events and
/// publishes them to the Azure Service Bus in order, marking each as processed or failed.
/// Uses IServiceScopeFactory so every batch runs in its own DI scope (scoped IOutboxPoller).
/// The worker is enabled only when BillingService:Modernization:EnableOutboxPublisher is true.
/// </summary>
public sealed class OutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisherWorker> logger)
    : BackgroundService
{
    private const int BatchSize = 20;
    private static readonly TimeSpan IdlePollInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan BusyPollInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BillingService outbox publisher worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = 0;
            try
            {
                processed = await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception in outbox publisher worker batch. Retrying after idle interval.");
            }

            // Back off when idle to reduce DB polling pressure; stay aggressive when work is available.
            var delay = processed > 0 ? BusyPollInterval : IdlePollInterval;
            await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
        }

        logger.LogInformation("BillingService outbox publisher worker stopped.");
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var poller = scope.ServiceProvider.GetRequiredService<IOutboxPoller>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var messages = await poller.FetchPendingAsync(BatchSize, cancellationToken).ConfigureAwait(false);
        if (messages.Count == 0)
        {
            return 0;
        }

        logger.LogInformation("Processing outbox batch of {Count} messages.", messages.Count);

        var processedCount = 0;
        foreach (var message in messages)
        {
            try
            {
                // Deserialize and re-publish via the event bus.
                // BillingOperationCompletedEvent is the canonical type; extend as new event types are added.
                var integrationEvent = DeserializeEvent(message);
                if (integrationEvent is not null)
                {
                    await eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
                }

                await poller.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
                processedCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to publish outbox message {MessageId} of type {EventType}. Attempt {AttemptCount}.",
                    message.Id, message.EventType, message.AttemptCount + 1);

                await poller.MarkFailedAsync(message.Id, ex.Message, cancellationToken).ConfigureAwait(false);
            }
        }

        return processedCount;
    }

    private static BillingService.Contracts.Events.IntegrationEvent? DeserializeEvent(OutboxMessage message)
    {
        // Type-based dispatch: extend this switch as new event types are introduced.
        return message.EventType switch
        {
            "billing.operation.completed" =>
                System.Text.Json.JsonSerializer.Deserialize<BillingService.Contracts.Events.BillingOperationCompletedEvent>(
                    message.Payload,
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)),
            _ => null
        };
    }
}
