namespace ClearingHouse.SharedKernel.Messaging;

public interface IMessageConsumer<in TMessage> where TMessage : class
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
}
