using ClearingHouseService.Domain.ValueObjects;

namespace ClearingHouseService.Domain.Entities
{
    /// <summary>
    /// Represents an EDI transaction that tracks the lifecycle of an EDI file
    /// from generation through submission and response processing.
    /// </summary>
    public class EdiTransaction
    {
        public Guid TransactionId { get; set; }
        public int ClaimId { get; set; }
        public int ClearingHouseId { get; set; }
        public EdiFormat Format { get; set; } = EdiFormat.Edi837P;
        public TransactionType Type { get; set; } = TransactionType.ClaimSubmission;
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
        public string? EdiContent { get; set; }
        public string? FileName { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string? CorrelationId { get; set; }
        public int RetryCount { get; set; }

        public static EdiTransaction Create(int claimId, int clearingHouseId, EdiFormat format, TransactionType type)
        {
            return new EdiTransaction
            {
                TransactionId = Guid.NewGuid(),
                ClaimId = claimId,
                ClearingHouseId = clearingHouseId,
                Format = format,
                Type = type,
                Status = TransactionStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString()
            };
        }

        public void MarkAsSubmitted(string fileName)
        {
            Status = TransactionStatus.Submitted;
            FileName = fileName;
        }

        public void MarkAsCompleted()
        {
            Status = TransactionStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }

        public void MarkAsFailed(string errorMessage)
        {
            Status = TransactionStatus.Failed;
            ErrorMessage = errorMessage;
            CompletedAt = DateTime.UtcNow;
        }

        public void IncrementRetry()
        {
            RetryCount++;
        }
    }

    /// <summary>
    /// Status of an EDI transaction.
    /// </summary>
    public enum TransactionStatus
    {
        Pending,
        Generating,
        Submitted,
        Processing,
        Completed,
        Failed,
        Retrying
    }
}
