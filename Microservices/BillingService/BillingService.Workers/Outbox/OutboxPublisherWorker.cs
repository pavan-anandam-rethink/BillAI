using BillingService.Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BillingService.Workers.Outbox;

public sealed class OutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxPublisherOptions> options,
    ILogger<OutboxPublisherWorker> logger)
    : BackgroundService
{
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
                // Graceful shutdown — exit cleanly.
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in outbox publisher worker. Retrying after delay.");
            }

            await Task.Delay(options.Value.PollingIntervalSeconds * 1_000, stoppingToken)
                .ConfigureAwait(false);
        }

        logger.LogInformation("BillingService outbox publisher worker stopped.");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        // Use a fresh scope per batch so scoped EF/ADO.NET contexts are not shared across
        // polling iterations and connections are returned to the pool promptly.
        await using var scope = scopeFactory.CreateAsyncScope();
        var reader   = scope.ServiceProvider.GetRequiredService<IOutboxReader>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var messages = await reader.ReadPendingAsync(options.Value.BatchSize, cancellationToken)
            .ConfigureAwait(false);

        if (messages.Count == 0)
        {
            return;
        }

        logger.LogInformation("Outbox worker processing {Count} pending message(s).", messages.Count);

        foreach (var message in messages)
        {
            await PublishMessageAsync(message, eventBus, reader, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task PublishMessageAsync(
        OutboxMessage message,
        IEventBus eventBus,
        IOutboxReader reader,
        CancellationToken cancellationToken)
    {
        try
        {
            // Reconstruct the typed integration event from the stored JSON payload.
            // The EventType is the discriminator; unknown types are skipped with a warning.
            var eventType = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name.Equals(message.EventType, StringComparison.Ordinal)
                                  || t.FullName?.EndsWith(message.EventType, StringComparison.Ordinal) == true);

            if (eventType is null)
            {
                logger.LogWarning(
                    "Outbox message {MessageId} has unknown event type '{EventType}'. Skipping.",
                    message.Id,
                    message.EventType);
                await reader.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
                return;
            }

            var integrationEvent = (Contracts.Events.IntegrationEvent?)JsonSerializer.Deserialize(
                message.Payload, eventType, SerializerOptions);

            if (integrationEvent is null)
            {
                logger.LogWarning(
                    "Outbox message {MessageId} payload deserialized to null. Skipping.", message.Id);
                await reader.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
                return;
            }

            await eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);

            await reader.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Outbox message {MessageId} ({EventType}) published successfully.",
                message.Id,
                message.EventType);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to publish outbox message {MessageId} ({EventType}). Attempt {Attempt}.",
                message.Id,
                message.EventType,
                message.AttemptCount + 1);

            await reader.RecordFailureAsync(message.Id, ex.Message, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}

