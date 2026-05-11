using ClearingHouse.SharedKernel.Domain;

namespace Reconciliation.Domain.Entities;

public class PaymentReconciliation : AggregateRoot
{
    public string ClaimId { get; private set; } = string.Empty;
    public string PayerClaimId { get; private set; } = string.Empty;
    public decimal ClaimedAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal AdjustmentAmount { get; private set; }
    public string PaymentStatus { get; private set; } = string.Empty;
    public Guid PaymentFileId { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public DateTime? PaymentDate { get; private set; }

    private PaymentReconciliation() { }

    public static PaymentReconciliation Create(string claimId, string payerClaimId, decimal claimedAmount, Guid paymentFileId, string correlationId)
    {
        return new PaymentReconciliation
        {
            ClaimId = claimId,
            PayerClaimId = payerClaimId,
            ClaimedAmount = claimedAmount,
            PaymentFileId = paymentFileId,
            CorrelationId = correlationId,
            PaymentStatus = "Pending"
        };
    }

    public void RecordPayment(decimal paidAmount, decimal adjustmentAmount, DateTime paymentDate)
    {
        PaidAmount = paidAmount;
        AdjustmentAmount = adjustmentAmount;
        PaymentDate = paymentDate;
        PaymentStatus = paidAmount >= ClaimedAmount ? "Paid" : "PartiallyPaid";
        IncrementVersion();
    }
}
