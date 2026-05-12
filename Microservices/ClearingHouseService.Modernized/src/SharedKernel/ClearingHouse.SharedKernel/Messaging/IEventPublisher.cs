namespace ClearingHouse.SharedKernel.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IIntegrationEvent;
    Task PublishAsync<T>(T @event, string topicName, CancellationToken cancellationToken = default) where T : IIntegrationEvent;
}
