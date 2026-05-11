using Microsoft.Extensions.Logging;

namespace StediIntegration.Infrastructure.Webhooks;

public class StediWebhookProcessor
{
    private readonly ILogger<StediWebhookProcessor> _logger;

    public StediWebhookProcessor(ILogger<StediWebhookProcessor> logger) => _logger = logger;

    public async Task ProcessAsync(string webhookId, string eventType, string payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing Stedi webhook {WebhookId}, EventType: {EventType}", webhookId, eventType);
        await Task.CompletedTask;
    }
}
