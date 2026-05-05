using Rethink.Services.Common.Entities.Base;
using System;

namespace Rethink.Services.Common.Entities.Billing.Reporting
{
    public class PaymentsAdjustmentsEntity : BasePersistEntity
    {
        public int AccountInfoId { get; set; }
        public int ClaimId { get; set; }
        public int ChargeEntryId { get; set; }
        public int? PaymentId { get; set; }
        public int FunderId { get; set; }
        public int ClientId { get; set; }
        public int ClaimStatusId { get; set; }
        public DateTime ClaimFrom { get; set; }
        public DateTime ClaimThrough { get; set; }
        public DateTime? BilledDate { get; set; }
        public int TransactionTypeId { get; set; }
        public int TransactionType { get; set; }
        public string? ReasonCode { get; set; }
        public string? RemarkCode { get; set; }
        public DateTime? TransactionDate { get; set; }
        public DateTime? PaymentOrAdjustmentDate { get; set; }
        public string? EftOrCheckNumber { get; set; }
        public Decimal Payment { get; set; }
        public Decimal Adjustment { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
}
