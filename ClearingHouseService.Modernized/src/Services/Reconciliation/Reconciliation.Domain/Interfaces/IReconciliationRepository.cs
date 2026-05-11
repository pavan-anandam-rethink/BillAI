using ClearingHouse.SharedKernel.Interfaces;
using Reconciliation.Domain.Entities;

namespace Reconciliation.Domain.Interfaces;

public interface IReconciliationRepository : IRepository<ReconciliationRecord>
{
    Task<IReadOnlyList<ReconciliationRecord>> GetUnmatchedAsync(int clearinghouseId, CancellationToken cancellationToken = default);
    Task<ReconciliationRecord?> GetByClaimIdAsync(string claimId, CancellationToken cancellationToken = default);
}
