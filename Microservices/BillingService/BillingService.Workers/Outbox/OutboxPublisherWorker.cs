using BillingService.Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BillingService.Workers.Outbox;

/// <summary>
/// Background worker that polls the durable outbox table and publishes unprocessed
/// integration events to Azure Service Bus on a 30-second cadence.
///
/// Each batch runs inside a short-lived DI scope so that scoped services (e.g. IOutboxPoller)
/// are resolved and disposed cleanly per iteration.
///
/// Delivery is idempotent: the ServiceBus MessageId is set to the outbox message GUID,
/// so duplicate deliveries caused by a restart mid-batch are deduplicated by the broker.
/// </summary>
public sealed class OutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisherWorker> logger)
    : BackgroundService
{
    private const int BatchSize = 20;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);

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
                logger.LogError(ex, "Unhandled error in BillingService outbox publisher; will retry after {Interval}.", PollingInterval);
            }

            await Task.Delay(PollingInterval, stoppingToken).ConfigureAwait(false);
        }

        logger.LogInformation("BillingService outbox publisher worker stopped.");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var poller = scope.ServiceProvider.GetRequiredService<IOutboxPoller>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var messages = await poller.FetchPendingAsync(BatchSize, cancellationToken).ConfigureAwait(false);

        if (messages.Count == 0)
        {
            return;
        }

        logger.LogDebug("BillingService outbox: processing {Count} pending message(s).", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                // Re-hydrate the event from its stored payload so IEventBus can set
                // the correct MessageId (EventId) for broker-level deduplication.
                var eventType = Type.GetType(message.EventType);
                if (eventType is null)
                {
                    logger.LogWarning(
                        "BillingService outbox: unknown EventType '{EventType}' for message {MessageId}; skipping.",
                        message.EventType,
                        message.Id);
                    await poller.MarkFailedAsync(message.Id, $"Unknown event type: {message.EventType}", cancellationToken).ConfigureAwait(false);
                    continue;
                }

                // Publish directly using the raw serialised payload wrapped in a minimal event envelope.
                // The real publish path via IEventBus.PublishAsync serialises the concrete type again,
                // so we use the stored EventType string as the Service Bus Subject directly.
                var integrationEvent = System.Text.Json.JsonSerializer.Deserialize(
                    message.Payload,
                    eventType,
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
                    as BillingService.Contracts.Events.IntegrationEvent;

                if (integrationEvent is null)
                {
                    await poller.MarkFailedAsync(message.Id, "Payload deserialised to null.", cancellationToken).ConfigureAwait(false);
                    continue;
                }

                await eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
                await poller.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);

                logger.LogInformation(
                    "BillingService outbox: published message {MessageId} ({EventType}).",
                    message.Id,
                    message.EventType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "BillingService outbox: failed to publish message {MessageId}.", message.Id);
                await poller.MarkFailedAsync(message.Id, ex.Message, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

