namespace ClearingHouse.SharedKernel.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(string containerName, string blobName, Stream content, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task<IDictionary<string, string>> GetMetadataAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task SetMetadataAsync(string containerName, string blobName, IDictionary<string, string> metadata, CancellationToken cancellationToken = default);
    Task MoveAsync(string sourceContainer, string sourceBlobName, string destContainer, string destBlobName, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> ListBlobsAsync(string containerName, string? prefix = null, CancellationToken cancellationToken = default);
}
