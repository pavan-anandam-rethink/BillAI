using BillingService.Application.Abstractions.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BillingService.Workers.Outbox;

public sealed class OutboxPublisherWorker(
    IEventBus eventBus,
    ILogger<OutboxPublisherWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BillingService outbox publisher worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // The first migration slice wires the worker contract only. Reading durable outbox
            // rows is enabled after the legacy write paths persist events transactionally.
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);
        }

        GC.KeepAlive(eventBus);
    }
}
