using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Payment
{
    public class PaymentAttachmentEntity : BasePersistEntity, IAuditedEntity
    {
        public int PaymentId { get; set; }
        public string FileName { get; set; }
        public string BlobFileName { get; set; }
        public double FileSize { get; set; }
        public string FileMimeType { get; set; }
        public string FilePath { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        [NotMapped]
        public virtual RethinkAccountMember Member { get; set; }
    }
}