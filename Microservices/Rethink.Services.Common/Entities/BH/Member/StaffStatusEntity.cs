using Rethink.Services.Common.Entities.Base;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.BH.Member
{
    public class StaffStatusEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("accountInfoId")]
        public int AccountInfoId { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("isActive")]
        public bool IsActive { get; set; }


        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
}
