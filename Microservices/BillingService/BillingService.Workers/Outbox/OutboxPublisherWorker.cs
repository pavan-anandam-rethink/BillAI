using BillingService.Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BillingService.Workers.Outbox;

/// <summary>
/// Background worker that polls the durable billing.OutboxMessages table and publishes
/// integration events to Azure Service Bus with at-least-once delivery semantics.
/// Resolves <see cref="IOutboxPoller"/> per batch via <see cref="IServiceScopeFactory"/>
/// to avoid holding a scoped database connection across poll intervals.
/// </summary>
public sealed class OutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    IEventBus eventBus,
    ILogger<OutboxPublisherWorker> logger)
    : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ErrorBackoff = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BillingService outbox publisher worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
                await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox publisher encountered an unexpected error. Backing off for {Seconds}s.",
                    ErrorBackoff.TotalSeconds);

                try
                {
                    await Task.Delay(ErrorBackoff, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        logger.LogInformation("BillingService outbox publisher worker stopped.");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        // Resolve IOutboxPoller in a dedicated scope so that the scoped DbConnection
        // (IBillingSqlConnectionFactory) is released at the end of each batch rather than
        // held for the lifetime of the background service.
        await using var scope = scopeFactory.CreateAsyncScope();
        var poller = scope.ServiceProvider.GetRequiredService<IOutboxPoller>();

        IReadOnlyCollection<OutboxMessage> messages;
        try
        {
            messages = await poller.PollAsync(BatchSize, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to poll outbox messages from SQL.");
            return;
        }

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await PublishMessageAsync(poller, message, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task PublishMessageAsync(
        IOutboxPoller poller,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            // Deserialize the stored JSON payload back into a typed integration event envelope
            // and publish it through the IEventBus abstraction.
            var integrationEvent = OutboxMessageSerializer.Deserialize(message);
            if (integrationEvent is null)
            {
                logger.LogWarning(
                    "Outbox message {MessageId} of type {EventType} could not be deserialized. Marking failed.",
                    message.Id, message.EventType);
                await poller.MarkFailedAsync(message.Id, "Deserialization returned null.", cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            await eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
            await poller.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Published outbox message {MessageId} of type {EventType}.",
                message.Id, message.EventType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to publish outbox message {MessageId} of type {EventType}.",
                message.Id, message.EventType);

            try
            {
                await poller.MarkFailedAsync(message.Id, ex.Message, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception markEx)
            {
                logger.LogError(markEx,
                    "Failed to mark outbox message {MessageId} as failed.", message.Id);
            }
        }
    }
}
