using Rethink.Services.Common.Entities.Base;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Entities.Billing.Payment
{
    [ExcludeFromCodeCoverage]
    public class UnAllocatedPaymentEntity : BasePersistEntity, IAuditedEntity
    {
        public int AccountInfoId { get; set; }
        public int PaymentId { get; set; }
        public int ChildProfileId { get; set; }
        public decimal UnAllocatedAmount { get; set; } = 0;
        public string? Notes { get; set; }
        public int? GuarantorContactId { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual PaymentEntity Payment { get; set; }
    }
}
