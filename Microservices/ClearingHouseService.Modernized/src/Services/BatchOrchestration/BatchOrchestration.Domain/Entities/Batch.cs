using ClearingHouse.SharedKernel.Domain;

namespace BatchOrchestration.Domain.Entities;

public class Batch : AggregateRoot<Guid>
{
    public string BatchName { get; private set; } = string.Empty;
    public string ClearinghouseId { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public BatchStatus Status { get; private set; } = BatchStatus.Created;
    public int TotalItems { get; private set; }
    public int CompletedItems { get; private set; }
    public int FailedItems { get; private set; }
    public int MaxConcurrency { get; private set; } = 10;
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public BatchPriority Priority { get; private set; } = BatchPriority.Normal;

    private readonly List<BatchItem> _items = new();
    public IReadOnlyCollection<BatchItem> Items => _items.AsReadOnly();

    private Batch() { }

    public static Batch Create(string batchName, string clearinghouseId, string correlationId,
        int maxConcurrency = 10, BatchPriority priority = BatchPriority.Normal)
    {
        return new Batch
        {
            Id = Guid.NewGuid(),
            BatchName = batchName,
            ClearinghouseId = clearinghouseId,
            CorrelationId = correlationId,
            MaxConcurrency = maxConcurrency,
            Priority = priority
        };
    }

    public BatchItem AddItem(Guid fileId, string fileName, string blobUri)
    {
        var item = new BatchItem(Guid.NewGuid(), Id, fileId, fileName, blobUri);
        _items.Add(item);
        TotalItems = _items.Count;
        IncrementVersion();
        return item;
    }

    public void Start()
    {
        Status = BatchStatus.Processing;
        StartedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    public void MarkItemCompleted(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        item?.MarkCompleted();
        CompletedItems = _items.Count(i => i.Status == BatchItemStatus.Completed);
        CheckCompletion();
    }

    public void MarkItemFailed(Guid itemId, string error)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        item?.MarkFailed(error);
        FailedItems = _items.Count(i => i.Status == BatchItemStatus.Failed);
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (CompletedItems + FailedItems >= TotalItems)
        {
            Status = FailedItems > 0 ? BatchStatus.CompletedWithErrors : BatchStatus.Completed;
            CompletedAt = DateTime.UtcNow;
            IncrementVersion();
            AddDomainEvent(new BatchCompletedEvent(Id, BatchName, ClearinghouseId, TotalItems, CompletedItems, FailedItems, CorrelationId));
        }
    }
}

public class BatchItem
{
    public Guid Id { get; private set; }
    public Guid BatchId { get; private set; }
    public Guid FileId { get; private set; }
    public string FileName { get; private set; }
    public string BlobUri { get; private set; }
    public BatchItemStatus Status { get; private set; } = BatchItemStatus.Pending;
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }

    public BatchItem(Guid id, Guid batchId, Guid fileId, string fileName, string blobUri)
    {
        Id = id;
        BatchId = batchId;
        FileId = fileId;
        FileName = fileName;
        BlobUri = blobUri;
    }

    public void MarkCompleted()
    {
        Status = BatchItemStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = BatchItemStatus.Failed;
        Error = error;
        ProcessedAt = DateTime.UtcNow;
    }
}

public enum BatchStatus { Created, Processing, Completed, CompletedWithErrors, Failed, Cancelled }
public enum BatchItemStatus { Pending, Processing, Completed, Failed }
public enum BatchPriority { Low = 0, Normal = 1, High = 2, Critical = 3 }

public record BatchCompletedEvent(Guid BatchId, string BatchName, string ClearinghouseId, int TotalItems, int CompletedItems, int FailedItems, string CorrelationId)
    : DomainEventBase;
