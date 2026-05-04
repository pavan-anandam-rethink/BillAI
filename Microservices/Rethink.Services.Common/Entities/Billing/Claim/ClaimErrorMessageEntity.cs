using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Enums.Billing;
using System;
using System.Collections.Generic;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class ClaimErrorMessageEntity : BasePersistEntity, IAuditedEntity
    {
        public string ShortDescription { get; set; }
        public string LongDescription { get; set; }

        public ClaimErrorNumber ErrorNumber { get; set; }
        public ClaimErrorSeverity Severity { get; set; }
        public int ClaimErrorCategoryId { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual ClaimErrorCategoryEntity ClaimErrorCategory { get; set; }
        public ICollection<ClaimValidationErrorEntity> ClaimValidationErrors { get; set; }
    }
}