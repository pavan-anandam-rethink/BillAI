using FileMetadata.Domain.Entities;

namespace FileMetadata.Domain.Interfaces;

public interface IFileEventHistoryRepository
{
    Task AddAsync(FileEventHistory eventHistory, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FileEventHistory>> GetByFileIdAsync(Guid fileId, CancellationToken cancellationToken = default);
}
