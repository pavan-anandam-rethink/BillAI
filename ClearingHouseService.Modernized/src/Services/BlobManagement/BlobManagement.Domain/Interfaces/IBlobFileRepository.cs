using ClearingHouse.SharedKernel.Interfaces;
using BlobManagement.Domain.Entities;

namespace BlobManagement.Domain.Interfaces;

public interface IBlobFileRepository : IRepository<BlobFile>
{
    Task<BlobFile?> GetByBlobNameAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlobFile>> GetByContainerAsync(string containerName, CancellationToken cancellationToken = default);
}
