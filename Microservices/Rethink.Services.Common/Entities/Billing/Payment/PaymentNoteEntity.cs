using Rethink.Services.Common.Entities.Base;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Payment
{
    public class PaymentNoteEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("hcPaymentId")]
        public int PaymentId { get; set; }
        public DateTime RemindDate { get; set; }
        public string Note { get; set; }
        public bool RecievedReminder { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }


        public virtual PaymentEntity Paymant { get; set; }
    }
}
