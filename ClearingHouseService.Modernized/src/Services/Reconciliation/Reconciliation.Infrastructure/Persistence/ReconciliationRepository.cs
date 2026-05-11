using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Reconciliation.Domain.Entities;
using Reconciliation.Domain.Interfaces;

namespace Reconciliation.Infrastructure.Persistence;

public class ReconciliationRepository : IReconciliationRepository
{
    private readonly ReconciliationDbContext _context;

    public ReconciliationRepository(ReconciliationDbContext context) => _context = context;

    public async Task<ReconciliationRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.ReconciliationRecords.FindAsync(new object[] { id }, cancellationToken);

    public async Task<IReadOnlyList<ReconciliationRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.ReconciliationRecords.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReconciliationRecord>> FindAsync(Expression<Func<ReconciliationRecord, bool>> predicate, CancellationToken cancellationToken = default) =>
        await _context.ReconciliationRecords.Where(predicate).ToListAsync(cancellationToken);

    public async Task<ReconciliationRecord> AddAsync(ReconciliationRecord entity, CancellationToken cancellationToken = default)
    {
        await _context.ReconciliationRecords.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(ReconciliationRecord entity, CancellationToken cancellationToken = default)
    {
        _context.ReconciliationRecords.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ReconciliationRecord entity, CancellationToken cancellationToken = default)
    {
        _context.ReconciliationRecords.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<ReconciliationRecord, bool>>? predicate = null, CancellationToken cancellationToken = default) =>
        predicate is null ? await _context.ReconciliationRecords.CountAsync(cancellationToken) : await _context.ReconciliationRecords.CountAsync(predicate, cancellationToken);

    public async Task<bool> ExistsAsync(Expression<Func<ReconciliationRecord, bool>> predicate, CancellationToken cancellationToken = default) =>
        await _context.ReconciliationRecords.AnyAsync(predicate, cancellationToken);

    public async Task<IReadOnlyList<ReconciliationRecord>> GetUnmatchedAsync(int clearinghouseId, CancellationToken cancellationToken = default) =>
        await _context.ReconciliationRecords.Where(r => r.ClearinghouseId == clearinghouseId && r.Status == "Submitted").ToListAsync(cancellationToken);

    public async Task<ReconciliationRecord?> GetByClaimIdAsync(string claimId, CancellationToken cancellationToken = default) =>
        await _context.ReconciliationRecords.FirstOrDefaultAsync(r => r.ClaimId == claimId, cancellationToken);
}
