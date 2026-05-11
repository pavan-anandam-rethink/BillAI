namespace ClearingHouse.SharedKernel.Interfaces;

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
    Task PublishAsync<T>(T @event, string topicOrQueue, CancellationToken cancellationToken = default) where T : class;
}
