using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using BlobManagement.Domain.Entities;
using BlobManagement.Domain.Interfaces;

namespace BlobManagement.Infrastructure.Persistence;

public class BlobFileRepository : IBlobFileRepository
{
    private readonly BlobManagementDbContext _context;

    public BlobFileRepository(BlobManagementDbContext context) => _context = context;

    public async Task<BlobFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.BlobFiles.FindAsync(new object[] { id }, cancellationToken);

    public async Task<IReadOnlyList<BlobFile>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.BlobFiles.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BlobFile>> FindAsync(Expression<Func<BlobFile, bool>> predicate, CancellationToken cancellationToken = default) =>
        await _context.BlobFiles.Where(predicate).ToListAsync(cancellationToken);

    public async Task<BlobFile> AddAsync(BlobFile entity, CancellationToken cancellationToken = default)
    {
        await _context.BlobFiles.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(BlobFile entity, CancellationToken cancellationToken = default)
    {
        _context.BlobFiles.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(BlobFile entity, CancellationToken cancellationToken = default)
    {
        _context.BlobFiles.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<BlobFile, bool>>? predicate = null, CancellationToken cancellationToken = default) =>
        predicate is null ? await _context.BlobFiles.CountAsync(cancellationToken) : await _context.BlobFiles.CountAsync(predicate, cancellationToken);

    public async Task<bool> ExistsAsync(Expression<Func<BlobFile, bool>> predicate, CancellationToken cancellationToken = default) =>
        await _context.BlobFiles.AnyAsync(predicate, cancellationToken);

    public async Task<BlobFile?> GetByBlobNameAsync(string containerName, string blobName, CancellationToken cancellationToken = default) =>
        await _context.BlobFiles.FirstOrDefaultAsync(f => f.ContainerName == containerName && f.BlobName == blobName, cancellationToken);

    public async Task<IReadOnlyList<BlobFile>> GetByContainerAsync(string containerName, CancellationToken cancellationToken = default) =>
        await _context.BlobFiles.Where(f => f.ContainerName == containerName).ToListAsync(cancellationToken);
}
