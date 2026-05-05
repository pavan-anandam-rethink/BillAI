using System;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;

namespace RethinkAutism.Data.Entities.Files
{
    public class FileCabinetEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("Id")]
        public int Id { get; set; }
        [Column("ChildProfileId")]
        public int? ChildProfileId { get; set; }
        [Column("FolderTypeId")]
        public int? FolderTypeId { get; set; }
        [Column("IsHealthcare")]
        public bool? IsHealthcare { get; set; }
        [Column("AccountInfoId")]
        public int? AccountInfoId { get; set; }
        [Column("MemberId")]
        public int? MemberId { get; set; }

        [Column("DateCreated")]
        public DateTime DateCreated { get; set; }
        [Column("DateLastModified")]
        public DateTime? DateLastModified { get; set; }
        [Column("DateDeleted")]
        public DateTime? DateDeleted { get; set; }
        [Column("CreatedBy")]
        public int CreatedBy { get; set; }
        [Column("ModifiedBy")]
        public int? ModifiedBy { get; set; }
        [Column("DeletedBy")]
        public int? DeletedBy { get; set; }

        public virtual FileCabinetFoldersEntity FolderType { get; set; }
    }
}
