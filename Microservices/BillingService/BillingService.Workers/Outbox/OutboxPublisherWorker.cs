using BillingService.Application.Abstractions.Messaging;
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
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private const int BatchSize = 20;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ErrorBackoff = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BillingService outbox publisher worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
                await Task.Delay(PollingInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox publisher encountered an error. Backing off for {Backoff}s.", ErrorBackoff.TotalSeconds);
                await Task.Delay(ErrorBackoff, stoppingToken).ConfigureAwait(false);
            }
        }

        logger.LogInformation("BillingService outbox publisher worker stopped.");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var poller = scope.ServiceProvider.GetRequiredService<IOutboxPoller>();

        var messages = await poller.PollAsync(BatchSize, cancellationToken).ConfigureAwait(false);

        if (messages.Count == 0)
        {
            return;
        }

        logger.LogInformation("Processing {Count} outbox messages.", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var integrationEvent = DeserializeEvent(message);
                if (integrationEvent is not null)
                {
                    await eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
                }

                await poller.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to publish outbox message {MessageId} of type {EventType}. Attempt {Attempt}.",
                    message.Id,
                    message.EventType,
                    message.AttemptCount + 1);

                await poller.MarkFailedAsync(message.Id, ex.Message, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private Contracts.Events.IntegrationEvent? DeserializeEvent(OutboxMessage message)
    {
        try
        {
            return JsonSerializer.Deserialize<Contracts.Events.BillingOperationCompletedEvent>(
                message.Payload, SerializerOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Could not deserialize outbox message {MessageId} as {EventType}.", message.Id, message.EventType);
            return null;
        }
    }
}
