using ClearingHouse.SharedKernel.Interfaces;
using FileTracking.Domain.Entities;

namespace FileTracking.Domain.Interfaces;

public interface IFileTrackingRepository : IRepository<FileTrackingRecord>
{
    Task<FileTrackingRecord?> GetByFileIdAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FileTrackingRecord>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FileTrackingRecord>> GetByClearinghouseAsync(int clearinghouseId, CancellationToken cancellationToken = default);
}
