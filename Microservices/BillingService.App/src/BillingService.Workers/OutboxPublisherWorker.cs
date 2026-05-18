using BillingService.App.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BillingService.App.Workers;

public sealed class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisherWorker> _logger;
    private readonly string _topicName;

    public OutboxPublisherWorker(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<OutboxPublisherWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _topicName = configuration["ServiceBus:OutboxTopic"] ?? "rt-billing-events";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dispatcher = scope.ServiceProvider.GetService<ServiceBusOutboxDispatcher>();
                if (dispatcher is not null)
                {
                    var sentCount = await dispatcher.DispatchAsync(_topicName, 100, stoppingToken).ConfigureAwait(false);
                    if (sentCount > 0)
                    {
                        _logger.LogInformation("Dispatched {Count} outbox events", sentCount);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatcher iteration failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);
        }
    }
}

