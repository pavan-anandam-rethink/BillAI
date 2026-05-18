using BillingService.Contracts;

namespace BillingService.Application.Abstractions;

public interface IOutboxWriter
{
    Task EnqueueAsync(BillingIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
