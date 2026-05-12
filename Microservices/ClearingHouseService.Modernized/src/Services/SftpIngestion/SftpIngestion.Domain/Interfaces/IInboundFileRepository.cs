using SftpIngestion.Domain.Entities;

namespace SftpIngestion.Domain.Interfaces;

public interface IInboundFileRepository
{
    Task<InboundFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InboundFile>> GetByClearinghouseAsync(string clearinghouseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InboundFile>> GetFailedFilesForRetryAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByHashAsync(string contentHash, string clearinghouseId, CancellationToken cancellationToken = default);
    Task AddAsync(InboundFile file, CancellationToken cancellationToken = default);
    Task UpdateAsync(InboundFile file, CancellationToken cancellationToken = default);
}
