using ClearingHouse.SharedKernel.Domain;
using ClearingHouse.SharedKernel.Enums;

namespace SftpIngestion.Domain.Entities;

public class IngestedFile : AggregateRoot
{
    public string FileName { get; private set; } = string.Empty;
    public string SourcePath { get; private set; } = string.Empty;
    public string BlobUri { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public int ClearinghouseId { get; private set; }
    public string ClearinghouseName { get; private set; } = string.Empty;
    public FileProcessingStatus Status { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public EdiTransactionType? TransactionType { get; private set; }
    public int RetryCount { get; private set; }
    public string? ErrorMessage { get; private set; }
    
    private IngestedFile() { }
    
    public static IngestedFile Create(string fileName, string sourcePath, int clearinghouseId, string clearinghouseName, string correlationId)
    {
        return new IngestedFile
        {
            FileName = fileName,
            SourcePath = sourcePath,
            ClearinghouseId = clearinghouseId,
            ClearinghouseName = clearinghouseName,
            CorrelationId = correlationId,
            Status = FileProcessingStatus.Pending
        };
    }
    
    public void MarkAsDownloaded(string blobUri, long fileSizeBytes, string contentHash)
    {
        BlobUri = blobUri;
        FileSizeBytes = fileSizeBytes;
        ContentHash = contentHash;
        Status = FileProcessingStatus.Downloaded;
        IncrementVersion();
    }
    
    public void MarkAsFailed(string errorMessage)
    {
        ErrorMessage = errorMessage;
        Status = FileProcessingStatus.Failed;
        RetryCount++;
        IncrementVersion();
    }
    
    public void SetTransactionType(EdiTransactionType transactionType)
    {
        TransactionType = transactionType;
        IncrementVersion();
    }
}
