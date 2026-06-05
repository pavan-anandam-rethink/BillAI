namespace BillingService.Application.Abstractions.Messaging;

public interface IOutboxPoller
{
    Task<IReadOnlyList<OutboxMessage>> PollAsync(
        int batchSize = 20,
        CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default);
}
