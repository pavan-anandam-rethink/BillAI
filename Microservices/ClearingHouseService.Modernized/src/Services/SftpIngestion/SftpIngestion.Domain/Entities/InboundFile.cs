using ClearingHouse.SharedKernel.Domain;

namespace SftpIngestion.Domain.Entities;

public class InboundFile : AggregateRoot<Guid>
{
    public string FileName { get; private set; } = string.Empty;
    public string SourcePath { get; private set; } = string.Empty;
    public string ClearinghouseId { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public string BlobUri { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public FileIngestionStatus Status { get; private set; } = FileIngestionStatus.Detected;
    public DateTime DetectedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? DownloadedAt { get; private set; }
    public DateTime? UploadedToBlobAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }

    private InboundFile() { }

    public static InboundFile Create(
        string fileName, string sourcePath, string clearinghouseId,
        long fileSizeBytes, string correlationId)
    {
        var file = new InboundFile
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            SourcePath = sourcePath,
            ClearinghouseId = clearinghouseId,
            FileSizeBytes = fileSizeBytes,
            CorrelationId = correlationId
        };

        file.AddDomainEvent(new FileDetectedEvent(file.Id, fileName, clearinghouseId, correlationId));
        return file;
    }

    public void MarkDownloaded(string contentHash)
    {
        Status = FileIngestionStatus.Downloaded;
        ContentHash = contentHash;
        DownloadedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    public void MarkUploadedToBlob(string blobUri)
    {
        Status = FileIngestionStatus.UploadedToBlob;
        BlobUri = blobUri;
        UploadedToBlobAt = DateTime.UtcNow;
        IncrementVersion();
        AddDomainEvent(new FileUploadedToBlobEvent(Id, FileName, ClearinghouseId, blobUri, CorrelationId));
    }

    public void MarkFailed(string error)
    {
        Status = FileIngestionStatus.Failed;
        LastError = error;
        RetryCount++;
        IncrementVersion();
        AddDomainEvent(new FileIngestionFailedEvent(Id, FileName, ClearinghouseId, error, RetryCount, CorrelationId));
    }

    public bool CanRetry() => RetryCount < 3;
}

public enum FileIngestionStatus
{
    Detected = 0,
    Downloading = 1,
    Downloaded = 2,
    UploadingToBlob = 3,
    UploadedToBlob = 4,
    Published = 5,
    Failed = 99
}

public record FileDetectedEvent(Guid FileId, string FileName, string ClearinghouseId, string CorrelationId)
    : DomainEventBase;

public record FileUploadedToBlobEvent(Guid FileId, string FileName, string ClearinghouseId, string BlobUri, string CorrelationId)
    : DomainEventBase;

public record FileIngestionFailedEvent(Guid FileId, string FileName, string ClearinghouseId, string Error, int RetryCount, string CorrelationId)
    : DomainEventBase;
