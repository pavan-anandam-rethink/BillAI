using System;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Member;

namespace Rethink.Services.Common.Entities.BH.ChildProfile
{
    public class ClientStatusEntity : BasePersistEntity, IAuditedEntity
    {
        public int AccountInfoId { get; set; }
        public string StatusName { get; set; }
        public bool IsDemo { get; set; }
        public bool ClientsAreInactive { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual AccountInfoEntity AccountInfo { get; set; }
    }
}