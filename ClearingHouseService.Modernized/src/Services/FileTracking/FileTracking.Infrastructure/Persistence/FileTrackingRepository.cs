using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using FileTracking.Domain.Entities;
using FileTracking.Domain.Interfaces;

namespace FileTracking.Infrastructure.Persistence;

public class FileTrackingRepository : IFileTrackingRepository
{
    private readonly FileTrackingDbContext _context;

    public FileTrackingRepository(FileTrackingDbContext context) => _context = context;

    public async Task<FileTrackingRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.FileTrackingRecords.Include(r => r.Timeline).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<FileTrackingRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.FileTrackingRecords.Include(r => r.Timeline).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FileTrackingRecord>> FindAsync(Expression<Func<FileTrackingRecord, bool>> predicate, CancellationToken cancellationToken = default) =>
        await _context.FileTrackingRecords.Include(r => r.Timeline).Where(predicate).ToListAsync(cancellationToken);

    public async Task<FileTrackingRecord> AddAsync(FileTrackingRecord entity, CancellationToken cancellationToken = default)
    {
        await _context.FileTrackingRecords.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(FileTrackingRecord entity, CancellationToken cancellationToken = default)
    {
        _context.FileTrackingRecords.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(FileTrackingRecord entity, CancellationToken cancellationToken = default)
    {
        _context.FileTrackingRecords.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<FileTrackingRecord, bool>>? predicate = null, CancellationToken cancellationToken = default) =>
        predicate is null ? await _context.FileTrackingRecords.CountAsync(cancellationToken) : await _context.FileTrackingRecords.CountAsync(predicate, cancellationToken);

    public async Task<bool> ExistsAsync(Expression<Func<FileTrackingRecord, bool>> predicate, CancellationToken cancellationToken = default) =>
        await _context.FileTrackingRecords.AnyAsync(predicate, cancellationToken);

    public async Task<FileTrackingRecord?> GetByFileIdAsync(Guid fileId, CancellationToken cancellationToken = default) =>
        await _context.FileTrackingRecords.Include(r => r.Timeline).FirstOrDefaultAsync(r => r.FileId == fileId, cancellationToken);

    public async Task<IReadOnlyList<FileTrackingRecord>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default) =>
        await _context.FileTrackingRecords.Include(r => r.Timeline).Where(r => r.CorrelationId == correlationId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FileTrackingRecord>> GetByClearinghouseAsync(int clearinghouseId, CancellationToken cancellationToken = default) =>
        await _context.FileTrackingRecords.Include(r => r.Timeline).Where(r => r.ClearinghouseId == clearinghouseId).ToListAsync(cancellationToken);
}
