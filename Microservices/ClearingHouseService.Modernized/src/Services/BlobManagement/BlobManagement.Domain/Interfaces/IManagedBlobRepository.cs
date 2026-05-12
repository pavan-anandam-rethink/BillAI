using BlobManagement.Domain.Entities;

namespace BlobManagement.Domain.Interfaces;

public interface IManagedBlobRepository
{
    Task<ManagedBlob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ManagedBlob?> GetByUriAsync(string blobUri, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagedBlob>> GetExpiredBlobsAsync(int batchSize = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagedBlob>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
    Task AddAsync(ManagedBlob blob, CancellationToken cancellationToken = default);
    Task UpdateAsync(ManagedBlob blob, CancellationToken cancellationToken = default);
}
