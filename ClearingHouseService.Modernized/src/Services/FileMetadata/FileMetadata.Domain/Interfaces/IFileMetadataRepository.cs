using ClearingHouse.SharedKernel.Interfaces;
using FileMetadata.Domain.Entities;

namespace FileMetadata.Domain.Interfaces;

public interface IFileMetadataRepository : IRepository<FileMetadataRecord>
{
    Task<IReadOnlyList<FileMetadataRecord>> GetByClearinghouseAsync(int clearinghouseId, CancellationToken cancellationToken = default);
    Task<FileMetadataRecord?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
}
