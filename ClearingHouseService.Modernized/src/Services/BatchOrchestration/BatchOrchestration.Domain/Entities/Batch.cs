using ClearingHouse.SharedKernel.Domain;
using ClearingHouse.SharedKernel.Enums;
using BatchOrchestration.Domain.Enums;

namespace BatchOrchestration.Domain.Entities;

public class Batch : AggregateRoot
{
    public BatchStatus Status { get; private set; }
    public int TotalFiles { get; private set; }
    public int ProcessedFiles { get; private set; }
    public int FailedFiles { get; private set; }
    public int ConcurrencyLimit { get; private set; }
    public BatchPriority Priority { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public int ClearinghouseId { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    private readonly List<BatchItem> _items = new();
    public IReadOnlyCollection<BatchItem> Items => _items.AsReadOnly();

    private Batch() { }

    public static Batch Create(int clearinghouseId, string correlationId, BatchPriority priority = BatchPriority.Normal, int concurrencyLimit = 5)
    {
        return new Batch
        {
            ClearinghouseId = clearinghouseId,
            CorrelationId = correlationId,
            Priority = priority,
            ConcurrencyLimit = concurrencyLimit,
            Status = BatchStatus.Created
        };
    }

    public BatchItem AddItem(Guid fileId, string fileName)
    {
        var item = BatchItem.Create(Id, fileId, fileName, _items.Count + 1);
        _items.Add(item);
        TotalFiles = _items.Count;
        IncrementVersion();
        return item;
    }

    public void Start()
    {
        Status = BatchStatus.InProgress;
        IncrementVersion();
    }

    public void RecordFileProcessed()
    {
        ProcessedFiles++;
        CheckCompletion();
        IncrementVersion();
    }

    public void RecordFileFailed()
    {
        FailedFiles++;
        CheckCompletion();
        IncrementVersion();
    }

    private void CheckCompletion()
    {
        if (ProcessedFiles + FailedFiles >= TotalFiles)
        {
            Status = FailedFiles > 0
                ? (ProcessedFiles > 0 ? BatchStatus.PartiallyCompleted : BatchStatus.Failed)
                : BatchStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }
    }

    public void Cancel()
    {
        Status = BatchStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        IncrementVersion();
    }
}
