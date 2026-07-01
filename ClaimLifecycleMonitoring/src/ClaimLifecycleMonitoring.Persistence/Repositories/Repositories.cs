using ClaimLifecycleMonitoring.Application.Abstractions.Persistence;
using ClaimLifecycleMonitoring.Domain.Entities;
using ClaimLifecycleMonitoring.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClaimLifecycleMonitoring.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IClaimRepository"/>.</summary>
public sealed class ClaimRepository : IClaimRepository
{
    private readonly ClaimLifecycleDbContext _db;

    /// <summary>Creates a new <see cref="ClaimRepository"/>.</summary>
    public ClaimRepository(ClaimLifecycleDbContext db) { _db = db; }

    /// <inheritdoc />
    public Task AddAsync(Claim claim, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(claim);
        return _db.Claims.AddAsync(claim, cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public async Task<Claim?> GetByIdAsync(long id, CancellationToken cancellationToken)
        => await _db.Claims
            .Include(c => c.History)
            .Include(c => c.Alerts)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Claim?> GetByClaimNumberAsync(string claimNumber, CancellationToken cancellationToken)
        => await _db.Claims
            .Include(c => c.History)
            .Include(c => c.Alerts)
            .FirstOrDefaultAsync(c => c.ClaimNumber == claimNumber, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string claimNumber, CancellationToken cancellationToken)
        => _db.Claims.AnyAsync(c => c.ClaimNumber == claimNumber, cancellationToken);

    /// <inheritdoc />
    public IQueryable<Claim> Query() => _db.Claims.AsNoTracking();

    /// <inheritdoc />
    public async Task<IReadOnlyList<Claim>> GetPotentiallyStuckAsync(DateTime olderThanUtc, int maxResults, CancellationToken cancellationToken)
    {
        var terminal = new[]
        {
            ClaimStatus.Completed,
            ClaimStatus.Cancelled,
            ClaimStatus.Duplicate
        };
        var list = await _db.Claims
            .Include(c => c.Alerts)
            .Where(c => !terminal.Contains(c.CurrentStatus))
            .Where(c => c.CreatedUtc >= olderThanUtc)
            .OrderBy(c => c.ModifiedUtc ?? c.CreatedUtc)
            .Take(maxResults)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return list;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Claim>> GetAwaitingInboundAsync(ClaimStage stage, DateTime olderThanUtc, int maxResults, CancellationToken cancellationToken)
    {
        var list = await _db.Claims
            .Include(c => c.Alerts)
            .Where(c => c.CurrentStage == stage && c.StageEnteredUtc <= olderThanUtc)
            .OrderBy(c => c.StageEnteredUtc)
            .Take(maxResults)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return list;
    }
}

/// <summary>EF Core implementation of <see cref="IAlertRepository"/>.</summary>
public sealed class AlertRepository : IAlertRepository
{
    private readonly ClaimLifecycleDbContext _db;

    /// <summary>Creates a new <see cref="AlertRepository"/>.</summary>
    public AlertRepository(ClaimLifecycleDbContext db) { _db = db; }

    /// <inheritdoc />
    public Task AddAsync(ClaimAlert alert, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(alert);
        return _db.Alerts.AddAsync(alert, cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public Task<ClaimAlert?> GetByIdAsync(long id, CancellationToken cancellationToken)
        => _db.Alerts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    /// <inheritdoc />
    public IQueryable<ClaimAlert> Query() => _db.Alerts.AsNoTracking();
}

/// <summary>EF Core implementation of <see cref="IStageSlaConfigurationRepository"/>.</summary>
public sealed class StageSlaConfigurationRepository : IStageSlaConfigurationRepository
{
    private readonly ClaimLifecycleDbContext _db;

    /// <summary>Creates a new <see cref="StageSlaConfigurationRepository"/>.</summary>
    public StageSlaConfigurationRepository(ClaimLifecycleDbContext db) { _db = db; }

    /// <inheritdoc />
    public async Task<StageSlaConfiguration> GetForStageAsync(ClaimStage stage, CancellationToken cancellationToken)
    {
        var cfg = await _db.StageSlaConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Stage == stage, cancellationToken)
            .ConfigureAwait(false);
        return cfg ?? new StageSlaConfiguration
        {
            Stage = stage,
            SlaHours = 24,
            BreachCategory = FailureCategory.SlaBreach,
            ExpectsInboundMessage = stage is ClaimStage.EdiSubmitted837 or ClaimStage.Received999 or ClaimStage.PayerProcessing
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StageSlaConfiguration>> GetAllAsync(CancellationToken cancellationToken)
    {
        var list = await _db.StageSlaConfigurations
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return list;
    }
}
