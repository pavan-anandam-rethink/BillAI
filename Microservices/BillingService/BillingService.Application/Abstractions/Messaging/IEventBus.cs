using BillingService.Contracts.Events;

namespace BillingService.Application.Abstractions.Messaging;

public interface IEventBus
{
    Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
