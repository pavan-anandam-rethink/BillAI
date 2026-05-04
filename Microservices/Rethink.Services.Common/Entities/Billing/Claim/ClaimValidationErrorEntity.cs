using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.Billing.Claim.Validation;
using Rethink.Services.Common.Enums.Billing;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class ClaimValidationErrorEntity : BasePersistEntity, IAuditedEntity
    {
        public string ContextMessage { get; set; }
        public int ClaimErrorMessageId { get; set; }
        public int? EraValidationErrorId { get; set; }
        public int? ClaimId { get; set; }
        public int? ClaimSubmissionId { get; set; }
        [Column("ClaimErrorSourceId")]
        public ClaimErrorSource ClaimErrorSource { get; set; }

        public DateTime ValidationDate { get; set; }
        public int? RefValidationId { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual ClaimEntity Claim { get; set; }
        public virtual ClaimSubmissionEntity ClaimSubmission { get; set; }
        public virtual ClaimErrorMessageEntity ClaimErrorMessage { get; set; }
        public virtual EraValidationErrorEntity EraValidationError { get; set; }
    }
}