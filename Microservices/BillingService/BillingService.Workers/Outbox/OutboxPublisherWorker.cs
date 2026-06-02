using BillingService.Application.Abstractions.Messaging;
using BillingService.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BillingService.Workers.Outbox;

/// <summary>
/// Background worker that polls the BillingOutboxMessages table for unprocessed
/// integration events and publishes them to Azure Service Bus with at-least-once
/// delivery semantics.
///
/// Design invariants:
/// - Polls on a fixed interval to minimise database pressure.
/// - Batch size is bounded to limit memory and transaction duration.
/// - ProcessedOnUtc is set via <see cref="IOutboxPoller.MarkProcessedAsync"/> AFTER a
///   successful Service Bus send to prevent data loss.
/// - Idempotent delivery is expected to be handled on the consumer side using EventId.
/// - Graceful shutdown when the host requests a stop.
/// </summary>
public sealed class OutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    IEventBus eventBus,
    ILogger<OutboxPublisherWorker> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BillingService outbox publisher worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishBatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "BillingService outbox: error during publish batch. Retrying after {PollIntervalSeconds}s.",
                    PollInterval.TotalSeconds);
            }

            await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
        }

        logger.LogInformation("BillingService outbox publisher worker stopped.");
    }

    private async Task PublishBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var poller = scope.ServiceProvider.GetRequiredService<IOutboxPoller>();

        var pending = await poller.ReadPendingAsync(BatchSize, cancellationToken).ConfigureAwait(false);
        if (pending.Count == 0)
        {
            return;
        }

        logger.LogInformation("BillingService outbox: publishing batch of {Count} events.", pending.Count);

        foreach (var message in pending)
        {
            try
            {
                var integrationEvent = DeserializeEvent(message);
                await eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);

                // Mark processed only after confirmed publish.
                await poller.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);

                logger.LogDebug(
                    "BillingService outbox: published and marked event {EventId} of type {EventType}.",
                    message.Id,
                    message.EventType);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The row stays unprocessed and will be retried in the next poll cycle.
                logger.LogError(ex,
                    "BillingService outbox: failed to publish event {EventId}. Will retry.",
                    message.Id);
            }
        }
    }

    private static IntegrationEvent DeserializeEvent(PendingOutboxMessage message)
    {
        // Attempt to resolve the original CLR type for a well-typed Service Bus message.
        var eventType = Type.GetType(message.EventType, throwOnError: false);
        if (eventType is not null && typeof(IntegrationEvent).IsAssignableFrom(eventType))
        {
            var deserialized = JsonSerializer.Deserialize(message.Payload, eventType, SerializerOptions);
            if (deserialized is IntegrationEvent typed)
            {
                return typed;
            }
        }

        // Fallback envelope preserves the raw payload so no event is silently dropped.
        return new GenericOutboxEvent(
            message.EventType,
            message.AggregateType,
            message.AggregateId,
            message.Payload)
        {
            EventId = message.Id,
            CorrelationId = message.CorrelationId
        };
    }

    /// <summary>
    /// Envelope for outbox rows whose CLR type cannot be resolved at runtime.
    /// Downstream consumers can inspect <see cref="RawPayload"/> to reprocess.
    /// </summary>
    private sealed record GenericOutboxEvent : IntegrationEvent
    {
        public GenericOutboxEvent(
            string eventType,
            string aggregateType,
            string aggregateId,
            string rawPayload)
            : base(eventType, aggregateType, aggregateId)
        {
            RawPayload = rawPayload;
        }

        public string RawPayload { get; init; }
    }
}

