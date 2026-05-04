using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Member;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.BH.Company
{
    //[AuditLog]
    public class PlaceOfServiceEntity : BasePersistEntity, IAuditedEntity, IEntity
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("accountInfoId")]
        public int AccountInfoId { get; set; }
        [Column("description")]
        public string Description { get; set; }
        [Column("code")]
        public string Code { get; set; }
        [Column("isActive")]
        public bool? IsActive { get; set; }

        [Column("dateCreated")]
        public DateTime DateCreated { get; set; }
        [Column("createdBy")]
        public int CreatedBy { get; set; }
        [Column("dateLastModified")]
        public DateTime? DateLastModified { get; set; }
        [Column("modifiedBy")]
        public int? ModifiedBy { get; set; }
        [Column("deletedBy")]
        public int? DeletedBy { get; set; }
        [Column("dateDeleted")]
        public DateTime? DateDeleted { get; set; }

        public AccountInfoEntity AccountInfo { get; set; }
    }
}
