using System;
using System.Collections.Generic;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Member;

namespace Rethink.Services.Common.Entities.BH.Company
{
    public class StateEntity : BasePersistEntity, IAuditedEntity
    {

        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public int? UtcOffSet { get; set; }
        public int? UtcDSTOffSet { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public virtual ICollection<ChildProfileEntity> ChildProfiles { get; set; }
    }
}