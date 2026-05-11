using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StediIntegration.Domain.Entities;
using StediIntegration.Domain.Interfaces;

namespace StediIntegration.Infrastructure.Persistence;

public class StediTransactionRepository : IStediTransactionRepository
{
    private readonly StediIntegrationDbContext _context;

    public StediTransactionRepository(StediIntegrationDbContext context) => _context = context;

    public async Task<StediTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.StediTransactions.FindAsync(new object[] { id }, cancellationToken);

    public async Task<IReadOnlyList<StediTransaction>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.StediTransactions.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StediTransaction>> FindAsync(Expression<Func<StediTransaction, bool>> predicate, CancellationToken cancellationToken = default) =>
        await _context.StediTransactions.Where(predicate).ToListAsync(cancellationToken);

    public async Task<StediTransaction> AddAsync(StediTransaction entity, CancellationToken cancellationToken = default)
    {
        await _context.StediTransactions.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(StediTransaction entity, CancellationToken cancellationToken = default)
    {
        _context.StediTransactions.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(StediTransaction entity, CancellationToken cancellationToken = default)
    {
        _context.StediTransactions.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<StediTransaction, bool>>? predicate = null, CancellationToken cancellationToken = default) =>
        predicate is null ? await _context.StediTransactions.CountAsync(cancellationToken) : await _context.StediTransactions.CountAsync(predicate, cancellationToken);

    public async Task<bool> ExistsAsync(Expression<Func<StediTransaction, bool>> predicate, CancellationToken cancellationToken = default) =>
        await _context.StediTransactions.AnyAsync(predicate, cancellationToken);

    public async Task<StediTransaction?> GetByStediTransactionIdAsync(string stediTransactionId, CancellationToken cancellationToken = default) =>
        await _context.StediTransactions.FirstOrDefaultAsync(t => t.StediTransactionId == stediTransactionId, cancellationToken);

    public async Task<IReadOnlyList<StediTransaction>> GetPendingTransactionsAsync(CancellationToken cancellationToken = default) =>
        await _context.StediTransactions.Where(t => t.Status == "Submitted").ToListAsync(cancellationToken);
}
