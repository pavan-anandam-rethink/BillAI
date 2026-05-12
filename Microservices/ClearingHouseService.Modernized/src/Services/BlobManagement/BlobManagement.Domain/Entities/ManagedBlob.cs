using ClearingHouse.SharedKernel.Domain;

namespace BlobManagement.Domain.Entities;

public class ManagedBlob : AggregateRoot<Guid>
{
    public string ContainerName { get; private set; } = string.Empty;
    public string BlobName { get; private set; } = string.Empty;
    public string BlobUri { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string ContentType { get; private set; } = "application/octet-stream";
    public string ContentHash { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public BlobLifecycleState State { get; private set; } = BlobLifecycleState.Active;
    public DateTime? ArchivedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime? RetentionExpiresAt { get; private set; }
    public IDictionary<string, string> Metadata { get; private set; } = new Dictionary<string, string>();

    private ManagedBlob() { }

    public static ManagedBlob Create(string containerName, string blobName, string blobUri,
        long sizeBytes, string contentHash, string correlationId, string contentType = "application/octet-stream",
        IDictionary<string, string>? metadata = null, int retentionDays = 90)
    {
        return new ManagedBlob
        {
            Id = Guid.NewGuid(),
            ContainerName = containerName,
            BlobName = blobName,
            BlobUri = blobUri,
            SizeBytes = sizeBytes,
            ContentHash = contentHash,
            ContentType = contentType,
            CorrelationId = correlationId,
            Metadata = metadata ?? new Dictionary<string, string>(),
            RetentionExpiresAt = DateTime.UtcNow.AddDays(retentionDays)
        };
    }

    public void Archive(string archiveContainer, string archiveBlobName)
    {
        State = BlobLifecycleState.Archived;
        ArchivedAt = DateTime.UtcNow;
        IncrementVersion();
        AddDomainEvent(new BlobArchivedEvent(Id, ContainerName, BlobName, archiveContainer, archiveBlobName, CorrelationId));
    }

    public void MoveToFailed(string failedContainer)
    {
        State = BlobLifecycleState.Failed;
        IncrementVersion();
        AddDomainEvent(new BlobMovedToFailedEvent(Id, ContainerName, BlobName, failedContainer, CorrelationId));
    }

    public void MarkForDeletion()
    {
        State = BlobLifecycleState.PendingDeletion;
        IncrementVersion();
    }

    public void Delete()
    {
        State = BlobLifecycleState.Deleted;
        DeletedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    public bool IsRetentionExpired() => RetentionExpiresAt.HasValue && DateTime.UtcNow >= RetentionExpiresAt.Value;
}

public enum BlobLifecycleState
{
    Active = 0,
    Processing = 1,
    Archived = 2,
    Failed = 3,
    PendingDeletion = 4,
    Deleted = 5
}

public record BlobArchivedEvent(Guid BlobId, string SourceContainer, string SourceBlobName, string ArchiveContainer, string ArchiveBlobName, string CorrelationId)
    : DomainEventBase;

public record BlobMovedToFailedEvent(Guid BlobId, string SourceContainer, string SourceBlobName, string FailedContainer, string CorrelationId)
    : DomainEventBase;
