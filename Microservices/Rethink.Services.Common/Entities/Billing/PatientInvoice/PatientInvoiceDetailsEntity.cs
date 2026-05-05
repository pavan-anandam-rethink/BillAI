using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.Billing.Claim;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.PatientInvoice
{
    public partial class PatientInvoiceDetailsEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("patientInvoiceId")]
        public int InvoiceId { get; set; }
        [Column("ClaimChargeEntryId")]
        public int ChargeId { get; set; }
        
        [Column("billedAmount")]
        public decimal BilledAmount { get; set; }
        [Column("insurancePayments")]
        public decimal InsurancePayments { get; set; }
        [Column("adjustmentNonPR")]
        public decimal AdjustmentNonPatientResponsibility { get; set; }

        [Column("adjustmentPR")]
        public decimal AdjustmentPatientResponsibility { get; set; }

        [Column("patientPayments")]
        public decimal PatientPayments { get; set; }
        [Column("patientBalance")]
        public decimal PatientBalance { get; set; }       
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }

        public virtual PatientInvoiceEntity PatientInvoiceEntity { get; set; }
        public virtual ClaimChargeEntryEntity ChargeEntry { get; set; }

    }
}
