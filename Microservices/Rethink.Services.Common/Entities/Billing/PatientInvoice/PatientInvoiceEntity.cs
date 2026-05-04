using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.PatientInvoice
{
    public class PatientInvoiceEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("invoiceNumber")]
        public string InvoiceNumber { get; set; }
        [Column("accountInfoId")]
        public int AccountId { get; set; }
        [Column("childProfileId")]
        public int ClientId { get; set; }
        [Column("invoiceDate")]
        public DateTime InvoiceDate { get; set; }
        [Column("paymentDueDate")]
        public DateTime PaymentDueDate { get; set; }
        [Column("statusId")]
        public PatientInvoiceStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public virtual IEnumerable<PatientInvoiceDetailsEntity> PatientInvoiceDetailsEntity { get; set; }
        public virtual IEnumerable<PatientGuarantorEntity> PatientGuarantors { get; set; }
    }
}
