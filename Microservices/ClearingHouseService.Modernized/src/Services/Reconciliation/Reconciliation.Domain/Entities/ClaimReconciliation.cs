using ClearingHouse.SharedKernel.Domain;

namespace Reconciliation.Domain.Entities;

public class ClaimReconciliationRecord : AggregateRoot<Guid>
{
    public string ClaimId { get; private set; } = string.Empty;
    public string PatientControlNumber { get; private set; } = string.Empty;
    public string PayerClaimId { get; private set; } = string.Empty;
    public string ClearinghouseId { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public ReconciliationStatus Status { get; private set; } = ReconciliationStatus.Pending;
    public decimal? SubmittedAmount { get; private set; }
    public decimal? PaidAmount { get; private set; }
    public decimal? AdjustmentAmount { get; private set; }
    public string? AdjustmentReasonCode { get; private set; }
    public DateTime SubmittedAt { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? ReconciledAt { get; private set; }
    public string? SubmissionFileId { get; private set; }
    public string? ResponseFileId { get; private set; }
    public string? EraFileId { get; private set; }

    private ClaimReconciliationRecord() { }

    public static ClaimReconciliationRecord Create(
        string claimId, string patientControlNumber, string clearinghouseId,
        string correlationId, decimal submittedAmount, string submissionFileId)
    {
        return new ClaimReconciliationRecord
        {
            Id = Guid.NewGuid(),
            ClaimId = claimId,
            PatientControlNumber = patientControlNumber,
            ClearinghouseId = clearinghouseId,
            CorrelationId = correlationId,
            SubmittedAmount = submittedAmount,
            SubmittedAt = DateTime.UtcNow,
            SubmissionFileId = submissionFileId
        };
    }

    public void RecordAcknowledgment(string payerClaimId, string responseFileId)
    {
        PayerClaimId = payerClaimId;
        ResponseFileId = responseFileId;
        AcknowledgedAt = DateTime.UtcNow;
        Status = ReconciliationStatus.Acknowledged;
        IncrementVersion();
    }

    public void RecordPayment(decimal paidAmount, decimal adjustmentAmount, string? adjustmentReasonCode, string eraFileId)
    {
        PaidAmount = paidAmount;
        AdjustmentAmount = adjustmentAmount;
        AdjustmentReasonCode = adjustmentReasonCode;
        EraFileId = eraFileId;
        PaidAt = DateTime.UtcNow;
        Status = ReconciliationStatus.Paid;
        IncrementVersion();
    }

    public void Reconcile()
    {
        Status = ReconciliationStatus.Reconciled;
        ReconciledAt = DateTime.UtcNow;
        IncrementVersion();
        AddDomainEvent(new ClaimReconciledEvent(Id, ClaimId, PatientControlNumber, ClearinghouseId, PaidAmount, CorrelationId));
    }

    public void MarkFailed(string reason)
    {
        Status = ReconciliationStatus.Failed;
        IncrementVersion();
        AddDomainEvent(new ReconciliationFailedEvent(Id, ClaimId, ClearinghouseId, reason, CorrelationId));
    }
}

public enum ReconciliationStatus
{
    Pending = 0,
    Acknowledged = 1,
    Paid = 2,
    Reconciled = 3,
    Failed = 99
}

public record ClaimReconciledEvent(Guid ReconciliationId, string ClaimId, string PatientControlNumber, string ClearinghouseId, decimal? PaidAmount, string CorrelationId)
    : DomainEventBase;

public record ReconciliationFailedEvent(Guid ReconciliationId, string ClaimId, string ClearinghouseId, string Reason, string CorrelationId)
    : DomainEventBase;
