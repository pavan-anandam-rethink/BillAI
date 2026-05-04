using Rethink.Services.Common.Entities.Base;
using System;

namespace Rethink.Services.Common.Entities.Billing.Payment
{
    public partial class PaymentClaimAttachmentEntity : BasePersistEntity, IAuditedEntity
    {
        public int Id { get; set; }
        public int PaymentClaimAttachmentTypeId { get; set; }
        public string FileName { get; set; }
        public double FileSize { get; set; }
        public string FileMimeType { get; set; }
        public string Notes { get; set; }
        public string FilePath { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual PaymentClaimAttachmentTypeEntity PaymentClaimAttachmentType { get; set; }
    }
}
