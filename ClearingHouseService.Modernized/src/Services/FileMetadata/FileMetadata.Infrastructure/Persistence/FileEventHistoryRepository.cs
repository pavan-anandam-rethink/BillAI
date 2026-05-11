using Microsoft.EntityFrameworkCore;
using FileMetadata.Domain.Entities;
using FileMetadata.Domain.Interfaces;

namespace FileMetadata.Infrastructure.Persistence;

public class FileEventHistoryRepository : IFileEventHistoryRepository
{
    private readonly FileMetadataDbContext _context;

    public FileEventHistoryRepository(FileMetadataDbContext context) => _context = context;

    public async Task AddAsync(FileEventHistory eventHistory, CancellationToken cancellationToken = default)
    {
        await _context.FileEventHistories.AddAsync(eventHistory, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FileEventHistory>> GetByFileIdAsync(Guid fileId, CancellationToken cancellationToken = default) =>
        await _context.FileEventHistories.Where(e => e.FileId == fileId).OrderBy(e => e.OccurredAt).ToListAsync(cancellationToken);
}
