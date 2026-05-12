using Microsoft.EntityFrameworkCore;
using SftpIngestion.Domain.Entities;
using SftpIngestion.Domain.Interfaces;

namespace SftpIngestion.Infrastructure.Persistence;

public class InboundFileRepository : IInboundFileRepository
{
    private readonly SftpIngestionDbContext _context;

    public InboundFileRepository(SftpIngestionDbContext context) => _context = context;

    public async Task<InboundFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.InboundFiles.FindAsync(new object[] { id }, cancellationToken);

    public async Task<IReadOnlyList<InboundFile>> GetByClearinghouseAsync(string clearinghouseId, CancellationToken cancellationToken = default) =>
        await _context.InboundFiles.Where(f => f.ClearinghouseId == clearinghouseId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InboundFile>> GetFailedFilesForRetryAsync(CancellationToken cancellationToken = default) =>
        await _context.InboundFiles
            .Where(f => f.Status == FileIngestionStatus.Failed && f.RetryCount < 3)
            .OrderBy(f => f.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsByHashAsync(string contentHash, string clearinghouseId, CancellationToken cancellationToken = default) =>
        await _context.InboundFiles.AnyAsync(f => f.ContentHash == contentHash && f.ClearinghouseId == clearinghouseId, cancellationToken);

    public async Task AddAsync(InboundFile file, CancellationToken cancellationToken = default)
    {
        await _context.InboundFiles.AddAsync(file, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(InboundFile file, CancellationToken cancellationToken = default)
    {
        _context.InboundFiles.Update(file);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
