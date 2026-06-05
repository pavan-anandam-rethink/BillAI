namespace BillingService.Application.Abstractions.Blob;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BillingService:BlobStorage";

    public string ConnectionString { get; init; } = string.Empty;

    public string DefaultContainerName { get; init; } = "rtafiles";
}
