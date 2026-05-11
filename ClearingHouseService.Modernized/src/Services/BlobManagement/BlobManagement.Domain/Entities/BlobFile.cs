using ClearingHouse.SharedKernel.Domain;
using ClearingHouse.SharedKernel.Enums;

namespace BlobManagement.Domain.Entities;

public class BlobFile : AggregateRoot
{
    public string ContainerName { get; private set; } = string.Empty;
    public string BlobName { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public FileProcessingStatus Status { get; private set; }
    public string? RetentionPolicy { get; private set; }
    public DateTime? ArchivedAt { get; private set; }
    public IDictionary<string, string> Metadata { get; private set; } = new Dictionary<string, string>();

    private BlobFile() { }

    public static BlobFile Create(string containerName, string blobName, long fileSizeBytes, string contentHash, string contentType)
    {
        return new BlobFile
        {
            ContainerName = containerName,
            BlobName = blobName,
            FileSizeBytes = fileSizeBytes,
            ContentHash = contentHash,
            ContentType = contentType,
            Status = FileProcessingStatus.Uploaded
        };
    }

    public void Archive()
    {
        ArchivedAt = DateTime.UtcNow;
        Status = FileProcessingStatus.Archived;
        IncrementVersion();
    }

    public void SetRetentionPolicy(string policy)
    {
        RetentionPolicy = policy;
        IncrementVersion();
    }

    public void SetMetadata(IDictionary<string, string> metadata)
    {
        Metadata = metadata;
        IncrementVersion();
    }
}
