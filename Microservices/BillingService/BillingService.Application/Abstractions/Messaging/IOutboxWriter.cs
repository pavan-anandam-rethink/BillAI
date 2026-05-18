using BillingService.Contracts.Events;

namespace BillingService.Application.Abstractions.Messaging;

public interface IOutboxWriter
{
    Task AddAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
