using ClearingHouse.SharedKernel.Domain;
using ClearingHouse.SharedKernel.Enums;

namespace FileMetadata.Domain.Entities;

public class FileMetadataRecord : AggregateRoot
{
    public string FileName { get; private set; } = string.Empty;
    public string BlobUri { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public int ClearinghouseId { get; private set; }
    public string ClearinghouseName { get; private set; } = string.Empty;
    public EdiTransactionType? TransactionType { get; private set; }
    public FileProcessingStatus Status { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public int RetryCount { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? DownloadedAt { get; private set; }
    public DateTime? ProcessingStartedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private FileMetadataRecord() { }

    public static FileMetadataRecord Create(string fileName, string blobUri, long fileSizeBytes, string contentHash,
        int clearinghouseId, string clearinghouseName, string correlationId)
    {
        return new FileMetadataRecord
        {
            FileName = fileName,
            BlobUri = blobUri,
            FileSizeBytes = fileSizeBytes,
            ContentHash = contentHash,
            ClearinghouseId = clearinghouseId,
            ClearinghouseName = clearinghouseName,
            CorrelationId = correlationId,
            Status = FileProcessingStatus.Pending
        };
    }

    public void MarkAsProcessing()
    {
        Status = FileProcessingStatus.Processing;
        ProcessingStartedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    public void MarkAsProcessed()
    {
        Status = FileProcessingStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = FileProcessingStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;
        IncrementVersion();
    }

    public void SetTransactionType(EdiTransactionType transactionType)
    {
        TransactionType = transactionType;
        IncrementVersion();
    }
}
