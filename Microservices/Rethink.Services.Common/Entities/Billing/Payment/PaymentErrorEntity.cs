using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Enums.Billing;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Payment
{
    public partial class PaymentErrorEntity : BasePersistEntity, IAuditedEntity
    {
        public PaymentErrorEntity()
        {
        }

        [Column("hcPaymentId")]
        public int PaymentId { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsCleared { get; set; }
        public PaymentErrorSeverity Severity { get; set; }
        public int? ErrorType { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual PaymentEntity Payment { get; set; }
    }
}
