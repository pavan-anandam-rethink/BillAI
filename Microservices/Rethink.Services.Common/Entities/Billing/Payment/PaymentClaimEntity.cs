using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.Billing.Claim;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Payment
{
    public partial class PaymentClaimEntity : BasePersistEntity, IAuditedEntity
    {
        public PaymentClaimEntity()
        {
            PaymentClaimAdjustments = new HashSet<PaymentClaimAdjustmentEntity>();
            PaymentClaimServiceLines = new HashSet<PaymentClaimServiceLineEntity>();
            PaymentClaimErrors = new HashSet<PaymentClaimErrorEntity>();
        }

        [Column("hcPaymentId")]
        public int PaymentId { get; set; }
        public int ChildProfileId { get; set; }
        public string ClaimIdentifier { get; set; }
        [Column("hcClaimId")]
        public int? ClaimId { get; set; }
        [Column("hcClaimSubmissionId")]
        public int? ClaimSubmissionId { get; set; }
        public string ClaimIdentifierOrig { get; set; }
        // NOTE: ClaimStatus is the ERA Claim Status (primary, secondary, denied, reversal) - this is different than the overall ClaimStatus
        public string ClaimStatus { get; set; }
        public string ClaimStatusOrig { get; set; }
        public decimal? TotalCharge { get; set; }
        public decimal? TotalChargeOrig { get; set; }
        public decimal? TotalPayment { get; set; }
        public decimal? TotalPaymentOrig { get; set; }
        public decimal? PatientRespAmount { get; set; }
        public decimal? PatientRespAmountOrig { get; set; }
        public string FilingIndicator { get; set; }
        public string FilingIndicatorOrig { get; set; }
        public string ControlNumber { get; set; }
        public string ClientIdentifier { get; set; }
        public string ClientLastName { get; set; }
        public string ClientFirstName { get; set; }
        public string ClientMiddleName { get; set; }
        public string? RenderingProviderId { get; set; }
        public string RenderingProviderName { get; set; }
        public DateTime? ClaimDateFrom { get; set; }
        public DateTime? ClaimDateTo { get; set; }
        public DateTime? ClaimReceivedDate { get; set; }
        public bool IsReviewed { get; set; }
        public string PlaceOfService { get; set; }
        public string? PatientId { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual ClaimEntity Claim { get; set; }
        public virtual ClaimSubmissionEntity ClaimSubmission { get; set; }
        public virtual PaymentEntity Payment { get; set; }
        public virtual ICollection<PaymentClaimAdjustmentEntity> PaymentClaimAdjustments { get; set; }
        public virtual ICollection<PaymentClaimServiceLineEntity> PaymentClaimServiceLines { get; set; }
        public virtual ICollection<PaymentClaimErrorEntity> PaymentClaimErrors { get; set; }
    }
}
