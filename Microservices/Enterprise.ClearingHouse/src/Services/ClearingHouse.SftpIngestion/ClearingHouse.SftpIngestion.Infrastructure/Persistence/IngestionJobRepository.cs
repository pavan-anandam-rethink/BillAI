using ClearingHouse.SharedKernel.Domain.Interfaces;
using ClearingHouse.SftpIngestion.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClearingHouse.SftpIngestion.Infrastructure.Persistence;

/// <summary>
/// Repository implementation for <see cref="SftpIngestionJob"/> aggregate root.
/// </summary>
public sealed class IngestionJobRepository : IRepository<SftpIngestionJob>
{
    private readonly IngestionDbContext _dbContext;
    private readonly ILogger<IngestionJobRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionJobRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public IngestionJobRepository(IngestionDbContext dbContext, ILogger<IngestionJobRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public IQueryable<SftpIngestionJob> Query => _dbContext.IngestionJobs.AsNoTracking();

    /// <inheritdoc />
    public async Task<SftpIngestionJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving ingestion job {JobId}", id);
        return await _dbContext.IngestionJobs.FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SftpIngestionJob> AddAsync(SftpIngestionJob entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _logger.LogDebug("Adding ingestion job {JobId}", entity.Id);
        var entry = await _dbContext.IngestionJobs.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entry.Entity;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(SftpIngestionJob entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _logger.LogDebug("Updating ingestion job {JobId}", entity.Id);
        _dbContext.IngestionJobs.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(SftpIngestionJob entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _logger.LogDebug("Deleting ingestion job {JobId}", entity.Id);
        _dbContext.IngestionJobs.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
