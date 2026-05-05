using Rethink.Services.Common.Entities.Base;
using System;

namespace Rethink.Services.Common.Entities.BH.Member
{
    public class StaffTitleEntity : BasePersistEntity, IAuditedEntity
    {
        public int AccountInfoId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int RoleTypeId { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public AccountRoleEntity AccountRole { get; set; }
    }
}
