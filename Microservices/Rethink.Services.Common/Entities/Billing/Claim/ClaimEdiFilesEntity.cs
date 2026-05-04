using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.Billing.Payment;
using System;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class ClaimEdiFilesEntity : BasePersistEntity, IAuditedEntity
    {
        public int AccountInfoId { get; set; }
        public string FileType { get; set; }          // 837, 999, 277, 835
        public int? ClaimSubmissionId { get; set; }   // 837, 999, 277, 835
        public int ClaimId { get; set; }             // 999, 277, 835
        public int? PaymentId { get; set; }           // 835 only
        public string BlobFilePath { get; set; }

        // Auditing Fields
        public DateTime DateCreated { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? DateLastModified { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation Properties
        public virtual ClaimEntity Claim { get; set; }
        public virtual ClaimSubmissionEntity ClaimSubmission { get; set; }
        public virtual PaymentEntity Payment { get; set; }
    }
}
