using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class ClaimChargeEntryEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("hcClaimId")]
        public int ClaimId { get; set; }

        public DateTime DateOfService { get; set; }

        public string BillingCode { get; set; }

        [Column("billingCodeId")]
        public int? BillingCodeId { get; set; }

        public string Modifier1 { get; set; }
        public bool? IncludeOnClaimMod1 { get; set; }

        public string Modifier2 { get; set; }
        public bool? IncludeOnClaimMod2 { get; set; }

        public string Modifier3 { get; set; }
        public bool? IncludeOnClaimMod3 { get; set; }

        public string Modifier4 { get; set; }
        public bool? IncludeOnClaimMod4 { get; set; }

        public decimal Units { get; set; }

        [Column("hcUnitTypeId")]
        public int UnitTypeId { get; set; }

        public decimal? UnitRate { get; set; }

        public decimal Charges { get; set; }

        public string DiagnosisCode { get; set; }

        public string DiagnosisCode2 { get; set; }

        public string Description { get; set; }
        public string BillingCodeDescription { get; set; }

        public int? ServiceLineIdentifier { get; set; }
        [MaxLength(80)]
        public string? NoteText { get; set; }
        public int? NoteCreatedBy { get; set; }
        public DateTime? NoteCreatedDate { get; set; }
        public int RenderingProviderId { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public virtual ClaimEntity Claim { get; set; }
        [NotMapped]
        public virtual ClientUnitTypes UnitType { get; set; }
        public virtual ICollection<ChargePaymentEntity> ChargePayments { get; set; }
        public virtual ICollection<ClaimChargeEntryWriteOffEntity> ClaimChargeEntryWriteOffs { get; set; }
        [NotMapped]
        public int? TypeId { get; set; }

        public virtual IEnumerable<PatientInvoiceDetailsEntity> PatientInvoiceDetailsEntity { get; set; }
    }
}