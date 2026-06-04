using BillingService.Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BillingService.Workers.Outbox;

/// <summary>
/// Background worker that polls the durable billing.BillingOutbox table
/// and relays unprocessed messages to Azure Service Bus.
/// Provides at-least-once delivery with per-message retry and failure recording.
/// </summary>
public sealed class OutboxPublisherWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<OutboxPublisherWorker> logger)
    : BackgroundService
{
    private const int BatchSize = 20;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan IdlePollInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BillingService outbox publisher worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
                var delay = processed > 0 ? PollInterval : IdlePollInterval;
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unhandled error in outbox publisher worker. Waiting before retry.");
                await Task.Delay(IdlePollInterval, stoppingToken).ConfigureAwait(false);
            }
        }

        logger.LogInformation("BillingService outbox publisher worker stopped.");
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var poller = scope.ServiceProvider.GetRequiredService<IOutboxPoller>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var messages = await poller.PollAsync(BatchSize, cancellationToken).ConfigureAwait(false);
        if (messages.Count == 0)
        {
            return 0;
        }

        var succeeded = new List<Guid>(messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var integrationEvent = System.Text.Json.JsonSerializer.Deserialize<BillingService.Contracts.Events.BillingOperationCompletedEvent>(
                    message.Payload,
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

                if (integrationEvent is not null)
                {
                    await eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
                }

                succeeded.Add(message.Id);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to publish outbox message {MessageId} of type {EventType}",
                    message.Id,
                    message.EventType);

                await poller.RecordFailureAsync(
                    message.Id,
                    exception.Message,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        if (succeeded.Count > 0)
        {
            await poller.MarkProcessedAsync(succeeded, cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Outbox batch: {Published} published, {Failed} failed",
            succeeded.Count,
            messages.Count - succeeded.Count);

        return succeeded.Count;
    }
}
