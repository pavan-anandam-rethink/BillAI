using ClearingHouse.SharedKernel.Domain;
using ClearingHouse.SharedKernel.Enums;

namespace BatchOrchestration.Domain.Entities;

public class BatchItem : Entity
{
    public Guid BatchId { get; private set; }
    public Guid FileId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public FileProcessingStatus Status { get; private set; }
    public int SequenceNumber { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private BatchItem() { }

    public static BatchItem Create(Guid batchId, Guid fileId, string fileName, int sequenceNumber)
    {
        return new BatchItem
        {
            BatchId = batchId,
            FileId = fileId,
            FileName = fileName,
            SequenceNumber = sequenceNumber,
            Status = FileProcessingStatus.Pending
        };
    }

    public void MarkAsProcessing()
    {
        Status = FileProcessingStatus.Processing;
    }

    public void MarkAsProcessed()
    {
        Status = FileProcessingStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = FileProcessingStatus.Failed;
        ErrorMessage = errorMessage;
    }
}
