using Rethink.Services.Common.Entities.Base;
using System;
using System.Collections.Generic;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class ClaimErrorCategoryEntity : BasePersistEntity, IAuditedEntity
    {

        public string Name { get; set; }
        public string Prefix { get; set; }
        public int NumberBase { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual ICollection<ClaimErrorMessageEntity> ClaimErrorMessages { get; set; }
    }
}