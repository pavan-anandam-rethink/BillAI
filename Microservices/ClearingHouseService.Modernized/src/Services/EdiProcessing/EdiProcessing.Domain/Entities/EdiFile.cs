using ClearingHouse.SharedKernel.Domain;

namespace EdiProcessing.Domain.Entities;

public class EdiFile : AggregateRoot<Guid>
{
    public string FileName { get; private set; } = string.Empty;
    public string BlobUri { get; private set; } = string.Empty;
    public EdiFileType FileType { get; private set; }
    public string ClearinghouseId { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public EdiProcessingStatus Status { get; private set; } = EdiProcessingStatus.Queued;
    public int TotalSegments { get; private set; }
    public int ProcessedSegments { get; private set; }
    public int ErrorCount { get; private set; }
    public DateTime? ProcessingStartedAt { get; private set; }
    public DateTime? ProcessingCompletedAt { get; private set; }
    public string? LastError { get; private set; }
    public int RetryCount { get; private set; }
    public string? BatchId { get; private set; }

    private readonly List<EdiProcessingError> _errors = new();
    public IReadOnlyCollection<EdiProcessingError> Errors => _errors.AsReadOnly();

    private EdiFile() { }

    public static EdiFile Create(string fileName, string blobUri, EdiFileType fileType,
        string clearinghouseId, string correlationId, long fileSizeBytes, string? batchId = null)
    {
        return new EdiFile
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            BlobUri = blobUri,
            FileType = fileType,
            ClearinghouseId = clearinghouseId,
            CorrelationId = correlationId,
            FileSizeBytes = fileSizeBytes,
            BatchId = batchId
        };
    }

    public void StartProcessing()
    {
        Status = EdiProcessingStatus.Processing;
        ProcessingStartedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    public void UpdateProgress(int processedSegments, int totalSegments)
    {
        ProcessedSegments = processedSegments;
        TotalSegments = totalSegments;
        IncrementVersion();
    }

    public void Complete()
    {
        Status = EdiProcessingStatus.Completed;
        ProcessingCompletedAt = DateTime.UtcNow;
        IncrementVersion();
        AddDomainEvent(new EdiFileProcessedEvent(Id, FileName, FileType, ClearinghouseId, CorrelationId));
    }

    public void Fail(string error)
    {
        Status = EdiProcessingStatus.Failed;
        LastError = error;
        ErrorCount++;
        RetryCount++;
        ProcessingCompletedAt = DateTime.UtcNow;
        IncrementVersion();
        _errors.Add(new EdiProcessingError(error, DateTime.UtcNow));
        AddDomainEvent(new EdiFileFailedEvent(Id, FileName, FileType, ClearinghouseId, error, RetryCount, CorrelationId));
    }

    public bool CanRetry() => RetryCount < 5;
}

public enum EdiFileType
{
    Edi837 = 837,
    Edi835 = 835,
    Edi999 = 999,
    Edi277 = 277,
    Edi270 = 270,
    Edi271 = 271,
    ClaimsSummary = 100
}

public enum EdiProcessingStatus
{
    Queued = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    RetryPending = 4,
    DeadLettered = 5
}

public record EdiProcessingError(string Message, DateTime OccurredAt);

public record EdiFileProcessedEvent(Guid FileId, string FileName, EdiFileType FileType, string ClearinghouseId, string CorrelationId)
    : DomainEventBase;

public record EdiFileFailedEvent(Guid FileId, string FileName, EdiFileType FileType, string ClearinghouseId, string Error, int RetryCount, string CorrelationId)
    : DomainEventBase;
