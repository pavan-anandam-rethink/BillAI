using ClearingHouse.SharedKernel.Domain;
using ClearingHouse.SharedKernel.Enums;

namespace FileTracking.Domain.Entities;

public class FileTrackingRecord : AggregateRoot
{
    public Guid FileId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ClearinghouseName { get; private set; } = string.Empty;
    public int ClearinghouseId { get; private set; }
    public FileProcessingStatus CurrentStatus { get; private set; }
    public EdiTransactionType? TransactionType { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public string? BlobUri { get; private set; }
    public long FileSizeBytes { get; private set; }
    public Guid? BatchId { get; private set; }
    public DateTime? LastStatusChange { get; private set; }
    private readonly List<FileTimeline> _timeline = new();
    public IReadOnlyCollection<FileTimeline> Timeline => _timeline.AsReadOnly();

    private FileTrackingRecord() { }

    public static FileTrackingRecord Create(Guid fileId, string fileName, int clearinghouseId, string clearinghouseName, string correlationId)
    {
        var record = new FileTrackingRecord
        {
            FileId = fileId,
            FileName = fileName,
            ClearinghouseId = clearinghouseId,
            ClearinghouseName = clearinghouseName,
            CorrelationId = correlationId,
            CurrentStatus = FileProcessingStatus.Pending
        };
        record.AddTimelineEvent("Created", "File tracking record created");
        return record;
    }

    public void UpdateStatus(FileProcessingStatus status, string description)
    {
        CurrentStatus = status;
        LastStatusChange = DateTime.UtcNow;
        AddTimelineEvent(status.ToString(), description);
        IncrementVersion();
    }

    private void AddTimelineEvent(string eventType, string description)
    {
        _timeline.Add(FileTimeline.Create(Id, eventType, description));
    }
}
