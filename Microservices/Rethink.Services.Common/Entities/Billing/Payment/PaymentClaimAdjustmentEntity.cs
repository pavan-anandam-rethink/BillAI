using Rethink.Services.Common.Entities.Base;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Payment
{
    public partial class PaymentClaimAdjustmentEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("hcPaymentClaimId")]
        public int PaymentClaimId { get; set; }
        public string AdjustmentGroupCode { get; set; }
        public string AdjustmentGroupCodeOrig { get; set; }
        public string AdjustmentReasonCode { get; set; }
        public string AdjustmentReasonCodeOrig { get; set; }
        public decimal? AdjustmentAmount { get; set; }
        public decimal? AdjustmentAmountOrig { get; set; }
        public decimal? AdjustmentQuantity { get; set; }
        public decimal? AdjustmentQuantityOrig { get; set; }
        [Column("hcPaymentAdjustmentReasonId")]
        public int? PaymentAdjustmentReasonId { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual PaymentClaimEntity PaymentClaim { get; set; }
        public virtual PaymentAdjustmentReasonEntity PaymentAdjustmentReason { get; set; }
    }
}
