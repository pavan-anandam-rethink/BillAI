namespace BillingService.Application.Abstractions.BlobStorage;

/// <summary>
/// Represents a downloaded blob together with its metadata.
/// </summary>
public sealed record BlobDownloadResult(
    Stream Content,
    BlobMetadata Metadata,
    string BlobName,
    string ContainerName);
