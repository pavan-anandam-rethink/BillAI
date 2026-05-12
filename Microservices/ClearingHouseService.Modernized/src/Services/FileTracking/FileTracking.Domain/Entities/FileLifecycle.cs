using ClearingHouse.SharedKernel.Domain;

namespace FileTracking.Domain.Entities;

public class FileLifecycle : AggregateRoot<Guid>
{
    public string FileName { get; private set; } = string.Empty;
    public string ClearinghouseId { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public FileDirection Direction { get; private set; }
    public string? EdiType { get; private set; }
    public long FileSizeBytes { get; private set; }
    public string? BlobUri { get; private set; }
    public FileLifecycleStatus CurrentStatus { get; private set; } = FileLifecycleStatus.Detected;
    public DateTime? ArchivedAt { get; private set; }

    private readonly List<FileEvent> _events = new();
    public IReadOnlyCollection<FileEvent> Events => _events.AsReadOnly();

    private FileLifecycle() { }

    public static FileLifecycle Create(string fileName, string clearinghouseId, string correlationId,
        FileDirection direction, long fileSizeBytes, string? ediType = null)
    {
        var lifecycle = new FileLifecycle
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ClearinghouseId = clearinghouseId,
            CorrelationId = correlationId,
            Direction = direction,
            FileSizeBytes = fileSizeBytes,
            EdiType = ediType
        };
        lifecycle.AddEvent(FileLifecycleStatus.Detected, "File detected");
        return lifecycle;
    }

    public void AddEvent(FileLifecycleStatus status, string description, string? details = null)
    {
        var fileEvent = new FileEvent(Guid.NewGuid(), Id, status, description, details, DateTime.UtcNow);
        _events.Add(fileEvent);
        CurrentStatus = status;
        IncrementVersion();
    }

    public void SetBlobUri(string blobUri)
    {
        BlobUri = blobUri;
        IncrementVersion();
    }

    public void Archive()
    {
        ArchivedAt = DateTime.UtcNow;
        AddEvent(FileLifecycleStatus.Archived, "File archived");
    }
}

public class FileEvent
{
    public Guid Id { get; private set; }
    public Guid FileLifecycleId { get; private set; }
    public FileLifecycleStatus Status { get; private set; }
    public string Description { get; private set; }
    public string? Details { get; private set; }
    public DateTime OccurredAt { get; private set; }

    public FileEvent(Guid id, Guid fileLifecycleId, FileLifecycleStatus status, string description, string? details, DateTime occurredAt)
    {
        Id = id;
        FileLifecycleId = fileLifecycleId;
        Status = status;
        Description = description;
        Details = details;
        OccurredAt = occurredAt;
    }
}

public enum FileDirection { Inbound, Outbound }

public enum FileLifecycleStatus
{
    Detected = 0,
    Downloaded = 1,
    StoredInBlob = 2,
    Queued = 3,
    Processing = 4,
    Processed = 5,
    Reconciled = 6,
    Archived = 7,
    Failed = 99
}
