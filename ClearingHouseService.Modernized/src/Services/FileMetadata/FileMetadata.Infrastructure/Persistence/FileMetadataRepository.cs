using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using FileMetadata.Domain.Entities;
using FileMetadata.Domain.Interfaces;

namespace FileMetadata.Infrastructure.Persistence;

public class FileMetadataRepository : IFileMetadataRepository
{
    private readonly FileMetadataDbContext _context;

    public FileMetadataRepository(FileMetadataDbContext context) => _context = context;

    public async Task<FileMetadataRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.FileMetadataRecords.FindAsync(new object[] { id }, cancellationToken);

    public async Task<IReadOnlyList<FileMetadataRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.FileMetadataRecords.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FileMetadataRecord>> FindAsync(Expression<Func<FileMetadataRecord, bool>> predicate, CancellationToken cancellationToken = default) =>
        await _context.FileMetadataRecords.Where(predicate).ToListAsync(cancellationToken);

    public async Task<FileMetadataRecord> AddAsync(FileMetadataRecord entity, CancellationToken cancellationToken = default)
    {
        await _context.FileMetadataRecords.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(FileMetadataRecord entity, CancellationToken cancellationToken = default)
    {
        _context.FileMetadataRecords.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(FileMetadataRecord entity, CancellationToken cancellationToken = default)
    {
        _context.FileMetadataRecords.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<FileMetadataRecord, bool>>? predicate = null, CancellationToken cancellationToken = default) =>
        predicate is null ? await _context.FileMetadataRecords.CountAsync(cancellationToken) : await _context.FileMetadataRecords.CountAsync(predicate, cancellationToken);

    public async Task<bool> ExistsAsync(Expression<Func<FileMetadataRecord, bool>> predicate, CancellationToken cancellationToken = default) =>
        await _context.FileMetadataRecords.AnyAsync(predicate, cancellationToken);

    public async Task<IReadOnlyList<FileMetadataRecord>> GetByClearinghouseAsync(int clearinghouseId, CancellationToken cancellationToken = default) =>
        await _context.FileMetadataRecords.Where(f => f.ClearinghouseId == clearinghouseId).ToListAsync(cancellationToken);

    public async Task<FileMetadataRecord?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default) =>
        await _context.FileMetadataRecords.FirstOrDefaultAsync(f => f.CorrelationId == correlationId, cancellationToken);
}
