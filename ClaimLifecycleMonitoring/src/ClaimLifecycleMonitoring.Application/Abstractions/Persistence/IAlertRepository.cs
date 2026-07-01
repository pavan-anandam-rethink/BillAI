using ClaimLifecycleMonitoring.Domain.Entities;
using ClaimLifecycleMonitoring.Domain.Enums;

namespace ClaimLifecycleMonitoring.Application.Abstractions.Persistence;

/// <summary>
/// Repository abstraction for <see cref="ClaimAlert"/> read/write operations.
/// </summary>
public interface IAlertRepository
{
    /// <summary>Adds a new alert.</summary>
    Task AddAsync(ClaimAlert alert, CancellationToken cancellationToken);

    /// <summary>Gets an alert by identifier.</summary>
    Task<ClaimAlert?> GetByIdAsync(long id, CancellationToken cancellationToken);

    /// <summary>Returns the queryable projection used by list / search queries.</summary>
    IQueryable<ClaimAlert> Query();
}

/// <summary>
/// Repository abstraction for stage SLA configuration.
/// </summary>
public interface IStageSlaConfigurationRepository
{
    /// <summary>Returns the configured SLA for the given stage or a sensible default.</summary>
    Task<StageSlaConfiguration> GetForStageAsync(ClaimStage stage, CancellationToken cancellationToken);

    /// <summary>Returns all configured SLAs.</summary>
    Task<IReadOnlyList<StageSlaConfiguration>> GetAllAsync(CancellationToken cancellationToken);
}
