using ClaimLifecycleMonitoring.Domain.Entities;
using ClaimLifecycleMonitoring.Domain.Enums;

namespace ClaimLifecycleMonitoring.Application.Abstractions.Persistence;

/// <summary>
/// Repository abstraction for the <see cref="Claim"/> aggregate root.
/// </summary>
public interface IClaimRepository
{
    /// <summary>Adds a new claim.</summary>
    Task AddAsync(Claim claim, CancellationToken cancellationToken);

    /// <summary>Gets a claim by its persistent identifier including full history and alerts.</summary>
    Task<Claim?> GetByIdAsync(long id, CancellationToken cancellationToken);

    /// <summary>Gets a claim by claim number including full history and alerts.</summary>
    Task<Claim?> GetByClaimNumberAsync(string claimNumber, CancellationToken cancellationToken);

    /// <summary>Returns whether a claim exists with the given claim number.</summary>
    Task<bool> ExistsAsync(string claimNumber, CancellationToken cancellationToken);

    /// <summary>Returns the queryable, tracking-disabled projection used by list / search queries.</summary>
    IQueryable<Claim> Query();

    /// <summary>Returns claims that have been in their current stage longer than <paramref name="olderThanUtc"/> and are in progress or waiting.</summary>
    Task<IReadOnlyList<Claim>> GetPotentiallyStuckAsync(DateTime olderThanUtc, int maxResults, CancellationToken cancellationToken);

    /// <summary>Returns claims whose stage is one of the stages awaiting an inbound message.</summary>
    Task<IReadOnlyList<Claim>> GetAwaitingInboundAsync(ClaimStage stage, DateTime olderThanUtc, int maxResults, CancellationToken cancellationToken);
}
