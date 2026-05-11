using ClearingHouse.SharedKernel.Domain;
using ClearingHouse.SharedKernel.Enums;

namespace StediIntegration.Domain.Entities;

public class StediTransaction : AggregateRoot
{
    public string StediTransactionId { get; private set; } = string.Empty;
    public Guid FileId { get; private set; }
    public EdiTransactionType TransactionType { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public string Direction { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public string? ResponsePayload { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private StediTransaction() { }

    public static StediTransaction Create(string stediTransactionId, Guid fileId, EdiTransactionType transactionType, string direction, string correlationId)
    {
        return new StediTransaction
        {
            StediTransactionId = stediTransactionId,
            FileId = fileId,
            TransactionType = transactionType,
            Direction = direction,
            CorrelationId = correlationId,
            Status = "Submitted"
        };
    }

    public void RecordResponse(string responsePayload, string status)
    {
        ResponsePayload = responsePayload;
        Status = status;
        CompletedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = "Failed";
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
        IncrementVersion();
    }
}
