using Rethink.Services.Common.Entities.Base;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace RethinkAutism.Data.Entities.Files
{
    public class SharedFolderAccessEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("FileCabinetFolderId")]
        public int FileCabinetFolderId { get; set; }
        [Column("UserType")]
        public int UserType { get; set; }
        [Column("UserId")]
        public int UserId { get; set; }

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

        public virtual FileCabinetFoldersEntity FileCabinetFolder { get; set; }
    }
}




