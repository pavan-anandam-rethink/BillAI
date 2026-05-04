using Rethink.Services.Common.Entities.Base;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.BH.Member
{
    public class AccountRoleEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("accountInfoId")]
        public int AccountInfoId { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("description")]
        public string Description { get; set; }
        public int AuditId { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }

        public int? DefaultRoleTypeId { get; set; }
        [Column("isAdmin")]
        public bool? IsAdmin { get; set; }
        [Column("tblRoleId")]
        public int? RoleId { get; set; }
    }
}
