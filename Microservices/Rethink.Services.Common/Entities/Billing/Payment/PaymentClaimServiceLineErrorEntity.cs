using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Enums.Billing;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Payment
{
    public partial class PaymentClaimServiceLineErrorEntity : BasePersistEntity, IAuditedEntity
    {
        public PaymentClaimServiceLineErrorEntity()
        {
        }

        [Column("hcPaymentClaimServiceLineId")]
        public int PaymentClaimServiceLineId { get; set; }
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

        public virtual PaymentClaimServiceLineEntity PaymentClaimServiceLine { get; set; }
    }
}
