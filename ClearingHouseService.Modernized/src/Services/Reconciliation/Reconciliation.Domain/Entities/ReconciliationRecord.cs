using ClearingHouse.SharedKernel.Domain;

namespace Reconciliation.Domain.Entities;

public class ReconciliationRecord : AggregateRoot
{
    public string ClaimId { get; private set; } = string.Empty;
    public Guid SubmissionFileId { get; private set; }
    public Guid? ResponseFileId { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public int ClearinghouseId { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public string? ErrorMessage { get; private set; }
    public DateTime? MatchedAt { get; private set; }
    public DateTime SubmittedAt { get; private set; }

    private ReconciliationRecord() { }

    public static ReconciliationRecord Create(string claimId, Guid submissionFileId, int clearinghouseId, string correlationId)
    {
        return new ReconciliationRecord
        {
            ClaimId = claimId,
            SubmissionFileId = submissionFileId,
            ClearinghouseId = clearinghouseId,
            CorrelationId = correlationId,
            Status = "Submitted",
            SubmittedAt = DateTime.UtcNow
        };
    }

    public void MatchWithResponse(Guid responseFileId, string status)
    {
        ResponseFileId = responseFileId;
        Status = status;
        MatchedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = "Failed";
        ErrorMessage = errorMessage;
        IncrementVersion();
    }
}
