using Rethink.Services.Common.Entities.Base;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class ClaimAttachmentEntity : BasePersistEntity, IAuditedEntity
    {
        public string FileName { get; set; }
        public double FileSize { get; set; }
        public string FileMimeType { get; set; }
        public string Notes { get; set; }
        public string FilePath { get; set; }
        [Column("hcClaimId")]
        public int ClaimId { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual ClaimEntity Claim { get; set; }

    }
}
