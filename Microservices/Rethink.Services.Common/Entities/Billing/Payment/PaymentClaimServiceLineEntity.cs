using Rethink.Services.Common.Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Payment
{
    public partial class PaymentClaimServiceLineEntity : BasePersistEntity, IAuditedEntity
    {
        public PaymentClaimServiceLineEntity()
        {
            PaymentClaimServiceLineAdjustments = new HashSet<PaymentClaimServiceLineAdjustmentEntity>();
        }

        [Column("hcPaymentClaimId")]
        public int? PaymentClaimId { get; set; }
        public string ServiceCode { get; set; }
        public string ServiceCodeOrig { get; set; }
        [Column("hcClaimChargeEntryId")]
        public int? ClaimChargeEntryId { get; set; }
        public decimal? ChargeAmount { get; set; }
        public decimal? ChargeAmountOrig { get; set; }
        public decimal? PaymentAmount { get; set; }
        public decimal? PaymentAmountOrig { get; set; }
        public string ProcedureModifier1 { get; set; }
        public string ProcedureModifier1Orig { get; set; }
        public string ProcedureModifier2 { get; set; }
        public string ProcedureModifier2Orig { get; set; }
        public string ProcedureModifier3 { get; set; }
        public string ProcedureModifier3Orig { get; set; }
        public string ProcedureModifier4 { get; set; }
        public string ProcedureModifier4Orig { get; set; }
        public string ProcedureDesc { get; set; }
        public string ProcedureUnits { get; set; }
        public string ProcedureUnitsOrig { get; set; }
        public string ReplacementServiceCode { get; set; }
        public string ReplacementProcedureModifier1 { get; set; }
        public string ReplacementProcedureModifier2 { get; set; }
        public string ReplacementProcedureModifier3 { get; set; }
        public string ReplacementProcedureModifier4 { get; set; }
        public string ReplacementProcedureUnits { get; set; }
        public string ReplacementProcedureDesc { get; set; }
        public DateTime? DateOfService { get; set; }
        public DateTime? DateOfServiceOrig { get; set; }
        public DateTime? ServiceStartDate { get; set; }
        public DateTime? ServiceStartDateOrig { get; set; }
        public DateTime? ServiceEndDate { get; set; }
        public DateTime? ServiceEndDateOrig { get; set; }
        public string LineControlNumber { get; set; }
        public decimal? AllowedAmount { get; set; }
        public decimal? AllowedAmountOrig { get; set; }
        public decimal? ExpectedAmount { get; set; }
        public string RemittanceRemarkCode1 { get; set; }
        public string RemittanceRemarkCode1Orig { get; set; }
        public string RemittanceRemarkCode2 { get; set; }
        public string RemittanceRemarkCode2Orig { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual PaymentClaimEntity PaymentClaim { get; set; }
        public virtual ICollection<PaymentClaimServiceLineAdjustmentEntity> PaymentClaimServiceLineAdjustments { get; set; }
        public virtual ICollection<PaymentClaimServiceLineErrorEntity> PaymentClaimServiceLineErrors { get; set; }

    }
}
