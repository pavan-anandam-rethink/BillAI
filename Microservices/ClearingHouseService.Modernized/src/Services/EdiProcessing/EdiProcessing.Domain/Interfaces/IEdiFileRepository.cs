using EdiProcessing.Domain.Entities;

namespace EdiProcessing.Domain.Interfaces;

public interface IEdiFileRepository
{
    Task<EdiFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EdiFile>> GetByBatchIdAsync(string batchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EdiFile>> GetPendingRetryAsync(CancellationToken cancellationToken = default);
    Task AddAsync(EdiFile file, CancellationToken cancellationToken = default);
    Task UpdateAsync(EdiFile file, CancellationToken cancellationToken = default);
}
