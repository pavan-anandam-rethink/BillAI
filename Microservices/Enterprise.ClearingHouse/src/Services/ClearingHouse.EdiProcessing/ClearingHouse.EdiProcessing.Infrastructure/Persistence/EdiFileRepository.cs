using ClearingHouse.EdiProcessing.Domain.Entities;
using ClearingHouse.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClearingHouse.EdiProcessing.Infrastructure.Persistence;

/// <summary>
/// Repository implementation for <see cref="EdiFile"/> entities.
/// </summary>
public sealed class EdiFileRepository : IRepository<EdiFile>
{
    private readonly EdiProcessingDbContext _dbContext;
    private readonly ILogger<EdiFileRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EdiFileRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public EdiFileRepository(
        EdiProcessingDbContext dbContext,
        ILogger<EdiFileRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<EdiFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EdiFiles
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddAsync(EdiFile entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await _dbContext.EdiFiles.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Added EdiFile {FileId} to repository", entity.Id);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(EdiFile entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _dbContext.EdiFiles.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Updated EdiFile {FileId} in repository", entity.Id);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity is not null)
        {
            _dbContext.EdiFiles.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Deleted EdiFile {FileId} from repository", id);
        }
    }

    /// <summary>
    /// Adds a batch of EDI segments to the database using optimized bulk insert.
    /// </summary>
    /// <param name="segments">The segments to insert.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task AddSegmentsBatchAsync(
        IEnumerable<EdiSegment> segments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(segments);

        await _dbContext.EdiSegments.AddRangeAsync(segments, cancellationToken).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Batch inserted EDI segments to repository");
    }

    /// <summary>
    /// Gets all processing errors for a specific file.
    /// </summary>
    /// <param name="fileId">The file identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of processing errors.</returns>
    public async Task<IReadOnlyList<EdiProcessingError>> GetErrorsByFileIdAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EdiProcessingErrors
            .Where(e => e.FileId == fileId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all segments for a specific file.
    /// </summary>
    /// <param name="fileId">The file identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of segments ordered by sequence number.</returns>
    public async Task<IReadOnlyList<EdiSegment>> GetSegmentsByFileIdAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EdiSegments
            .Where(s => s.FileId == fileId)
            .OrderBy(s => s.SequenceNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
