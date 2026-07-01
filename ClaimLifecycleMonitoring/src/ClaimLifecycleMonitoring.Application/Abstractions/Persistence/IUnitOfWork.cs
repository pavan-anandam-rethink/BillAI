namespace ClaimLifecycleMonitoring.Application.Abstractions.Persistence;

/// <summary>
/// Encapsulates persistence transaction boundaries and change tracking commit.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Persists all pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
