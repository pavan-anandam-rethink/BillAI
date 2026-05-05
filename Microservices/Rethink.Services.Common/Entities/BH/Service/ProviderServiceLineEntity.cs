using System;
using System.Collections.Generic;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.ChildProfile;
using Rethink.Services.Common.Entities.BH.Member;

namespace Rethink.Services.Common.Entities.BH.Service
{
    public class ProviderServiceLineEntity : BasePersistEntity, IAuditedEntity
    {
        public int AccountInfoId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual AccountInfoEntity AccountInfo { get; set; }
        //public virtual List<ServiceFunderEntity> ServiceFunders { get; set; }
        //public virtual List<ChildProfileAuthorizationEntity> ChildProfileAuthorizations { get; set; }
        public virtual List<ChildProfileFunderServiceLineMappingEntity>    ChildProfileFunderServiceLineMapings { get; set; }
        public virtual ICollection<ChildProfileAuthorizationEntity> ChildProfileAuthorizations { get; set; }
    }
}