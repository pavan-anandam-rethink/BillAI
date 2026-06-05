namespace BillingService.Application.Abstractions.Blob;

public interface IBlobMetadataStore
{
    Task SaveAsync(BlobMetadata metadata, CancellationToken cancellationToken = default);

    Task<BlobMetadata?> FindByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BlobMetadata>> FindByTagAsync(
        string tagKey,
        string tagValue,
        CancellationToken cancellationToken = default);
}
