using System.Linq.Expressions;
using ClearingHouse.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;
using BatchOrchestration.Domain.Entities;
using BatchOrchestration.Domain.Interfaces;

namespace BatchOrchestration.Infrastructure.Persistence;

public class BatchRepository : IBatchRepository
{
    private readonly BatchOrchestrationDbContext _context;

    public BatchRepository(BatchOrchestrationDbContext context) => _context = context;

    public async Task<Batch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Batches.Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Batch>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Batches.Include(b => b.Items).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Batch>> FindAsync(Expression<Func<Batch, bool>> predicate, CancellationToken cancellationToken = default) =>
        await _context.Batches.Include(b => b.Items).Where(predicate).ToListAsync(cancellationToken);

    public async Task<Batch> AddAsync(Batch entity, CancellationToken cancellationToken = default)
    {
        await _context.Batches.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Batch entity, CancellationToken cancellationToken = default)
    {
        _context.Batches.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Batch entity, CancellationToken cancellationToken = default)
    {
        _context.Batches.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<Batch, bool>>? predicate = null, CancellationToken cancellationToken = default) =>
        predicate is null ? await _context.Batches.CountAsync(cancellationToken) : await _context.Batches.CountAsync(predicate, cancellationToken);

    public async Task<bool> ExistsAsync(Expression<Func<Batch, bool>> predicate, CancellationToken cancellationToken = default) =>
        await _context.Batches.AnyAsync(predicate, cancellationToken);

    public async Task<IReadOnlyList<Batch>> GetByStatusAsync(BatchStatus status, CancellationToken cancellationToken = default) =>
        await _context.Batches.Include(b => b.Items).Where(b => b.Status == status).ToListAsync(cancellationToken);

    public async Task<Batch?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default) =>
        await _context.Batches.Include(b => b.Items).FirstOrDefaultAsync(b => b.CorrelationId == correlationId, cancellationToken);

    public async Task<IReadOnlyList<Batch>> GetActiveBatchesAsync(CancellationToken cancellationToken = default) =>
        await _context.Batches.Include(b => b.Items)
            .Where(b => b.Status == BatchStatus.InProgress || b.Status == BatchStatus.Queued)
            .ToListAsync(cancellationToken);
}
