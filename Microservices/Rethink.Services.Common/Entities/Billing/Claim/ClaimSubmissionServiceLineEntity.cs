using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Enums.BH;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class ClaimSubmissionServiceLineEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("hcClaimSubmissionId")]
        public int ClaimSubmissionId { get; set; }

        [Column("hcClaimChargeEntryId")]
        public int ClaimChargeEntryId { get; set; }

        public DateTime? DateOfService { get; set; }
        public string BillingCode { get; set; }
        public string BillingCodeDescription { get; set; }
        public string Modifier1 { get; set; }
        public string Modifier2 { get; set; }
        public string Modifier3 { get; set; }
        public string Modifier4 { get; set; }
        public decimal? Units { get; set; }
        public decimal? UnitRate { get; set; }
        public string UnitType { get; set; }

        [Column("hcUnitTypeId")]
        public int? UnitTypeId { get; set; }
        public decimal? Charges { get; set; }
        public string DiagnosisCode { get; set; }
        public int? DiagnosisCodeOrder { get; set; }
        public DiagnosisTypes? DiagnosisCodeType { get; set; }
        public string ServiceLineIdentifier { get; set; }
        public int? ServiceLineIndex { get; set; }

        [MaxLength(80)]
        public string? NoteText { get; set; }
        public int? NoteCreatedBy { get; set; }
        public DateTime? NoteCreatedDate { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }


        public virtual ClaimSubmissionEntity ClaimSubmission { get; set; }
        public virtual ClaimChargeEntryEntity ClaimChargeEntry { get; set; }
    }
}