using Microsoft.EntityFrameworkCore;

namespace BillingService.App.Persistence;

public interface IOutboxRepository
{
    Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken);
    Task<IReadOnlyList<OutboxMessage>> DequeueBatchAsync(int batchSize, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly BillingOutboxDbContext _dbContext;

    public OutboxRepository(BillingOutboxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await _dbContext.OutboxMessages.AddAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OutboxMessage>> DequeueBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        return await _dbContext.OutboxMessages
            .AsNoTracking()
            .Where(x => x.ProcessedUtc == null)
            .OrderBy(x => x.CreatedUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}

