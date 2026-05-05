using System;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.BH.ChildProfile
{
    public class ChildProfileReferringProviderEntity : BasePersistEntity, IAuditedEntity
    {
        public int ChildProfileId { get; set; }
        public int ReferringProviderId { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual ReferringProviderEntity ReferringProvider { get; set; }
    }
}