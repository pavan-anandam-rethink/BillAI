using ClearingHouse.SharedKernel.Enums;
using ClearingHouse.SharedKernel.Interfaces;
using BatchOrchestration.Domain.Entities;

namespace BatchOrchestration.Domain.Interfaces;

public interface IBatchRepository : IRepository<Batch>
{
    Task<IReadOnlyList<Batch>> GetByStatusAsync(BatchStatus status, CancellationToken cancellationToken = default);
    Task<Batch?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Batch>> GetActiveBatchesAsync(CancellationToken cancellationToken = default);
}
