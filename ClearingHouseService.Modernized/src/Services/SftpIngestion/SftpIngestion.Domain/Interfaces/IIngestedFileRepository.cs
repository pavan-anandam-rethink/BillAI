using ClearingHouse.SharedKernel.Interfaces;
using SftpIngestion.Domain.Entities;

namespace SftpIngestion.Domain.Interfaces;

public interface IIngestedFileRepository : IRepository<IngestedFile>
{
    Task<IReadOnlyList<IngestedFile>> GetPendingFilesAsync(int clearinghouseId, CancellationToken cancellationToken = default);
    Task<IngestedFile?> GetByFileNameAndClearinghouseAsync(string fileName, int clearinghouseId, CancellationToken cancellationToken = default);
}
