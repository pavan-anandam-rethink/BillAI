using System.Text;
using Azure.Messaging.ServiceBus;
using BillingService.App.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingService.App.Infrastructure.Messaging;

public sealed class ServiceBusOutboxDispatcher
{
    private readonly BillingOutboxDbContext _dbContext;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<ServiceBusOutboxDispatcher> _logger;

    public ServiceBusOutboxDispatcher(
        BillingOutboxDbContext dbContext,
        ServiceBusClient serviceBusClient,
        ILogger<ServiceBusOutboxDispatcher> logger)
    {
        _dbContext = dbContext;
        _serviceBusClient = serviceBusClient;
        _logger = logger;
    }

    public async Task<int> DispatchAsync(string topicName, int batchSize, CancellationToken cancellationToken)
    {
        var pending = await _dbContext.OutboxMessages
            .Where(x => x.ProcessedUtc == null)
            .OrderBy(x => x.CreatedUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (pending.Count == 0)
        {
            return 0;
        }

        var sender = _serviceBusClient.CreateSender(topicName);

        foreach (var msg in pending)
        {
            try
            {
                var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(msg.PayloadJson))
                {
                    MessageId = msg.Id.ToString(),
                    CorrelationId = msg.CorrelationId,
                    Subject = msg.EventType
                };

                await sender.SendMessageAsync(serviceBusMessage, cancellationToken).ConfigureAwait(false);
                msg.ProcessedUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                msg.RetryCount++;
                _logger.LogError(ex, "Failed to dispatch outbox message {OutboxId}", msg.Id);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return pending.Count(x => x.ProcessedUtc.HasValue);
    }
}

