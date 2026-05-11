using System.Linq.Expressions;
using ClearingHouse.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;
using SftpIngestion.Domain.Entities;
using SftpIngestion.Domain.Interfaces;

namespace SftpIngestion.Infrastructure.Persistence;

public class IngestedFileRepository : IIngestedFileRepository
{
    private readonly SftpIngestionDbContext _context;

    public IngestedFileRepository(SftpIngestionDbContext context) => _context = context;

    public async Task<IngestedFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.IngestedFiles.FindAsync(new object[] { id }, cancellationToken);

    public async Task<IReadOnlyList<IngestedFile>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.IngestedFiles.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<IngestedFile>> FindAsync(Expression<Func<IngestedFile, bool>> predicate, CancellationToken cancellationToken = default) =>
        await _context.IngestedFiles.Where(predicate).ToListAsync(cancellationToken);

    public async Task<IngestedFile> AddAsync(IngestedFile entity, CancellationToken cancellationToken = default)
    {
        await _context.IngestedFiles.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(IngestedFile entity, CancellationToken cancellationToken = default)
    {
        _context.IngestedFiles.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(IngestedFile entity, CancellationToken cancellationToken = default)
    {
        _context.IngestedFiles.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<IngestedFile, bool>>? predicate = null, CancellationToken cancellationToken = default) =>
        predicate is null ? await _context.IngestedFiles.CountAsync(cancellationToken) : await _context.IngestedFiles.CountAsync(predicate, cancellationToken);

    public async Task<bool> ExistsAsync(Expression<Func<IngestedFile, bool>> predicate, CancellationToken cancellationToken = default) =>
        await _context.IngestedFiles.AnyAsync(predicate, cancellationToken);

    public async Task<IReadOnlyList<IngestedFile>> GetPendingFilesAsync(int clearinghouseId, CancellationToken cancellationToken = default) =>
        await _context.IngestedFiles
            .Where(f => f.ClearinghouseId == clearinghouseId && f.Status == FileProcessingStatus.Pending)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IngestedFile?> GetByFileNameAndClearinghouseAsync(string fileName, int clearinghouseId, CancellationToken cancellationToken = default) =>
        await _context.IngestedFiles
            .FirstOrDefaultAsync(f => f.FileName == fileName && f.ClearinghouseId == clearinghouseId, cancellationToken);
}
