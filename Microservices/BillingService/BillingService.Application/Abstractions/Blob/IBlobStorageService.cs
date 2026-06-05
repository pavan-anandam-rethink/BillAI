namespace BillingService.Application.Abstractions.Blob;

public interface IBlobStorageService
{
    Task UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType = "application/octet-stream",
        IDictionary<string, string>? metadata = null,
        IDictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    Task<IDictionary<string, string>> GetTagsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);
}
