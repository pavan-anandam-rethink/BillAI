using BlobManagement.Domain.Entities;
using ClearingHouse.SharedKernel.Domain;

namespace BlobManagement.Domain.Services;

public interface IBlobLifecycleService
{
    Task<Result<ManagedBlob>> RegisterBlobAsync(string containerName, string blobName, string blobUri,
        long sizeBytes, string contentHash, string correlationId, IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);
    Task<Result> ArchiveBlobAsync(Guid blobId, CancellationToken cancellationToken = default);
    Task<Result> MoveToFailedAsync(Guid blobId, CancellationToken cancellationToken = default);
    Task<Result<int>> CleanupExpiredBlobsAsync(CancellationToken cancellationToken = default);
}
