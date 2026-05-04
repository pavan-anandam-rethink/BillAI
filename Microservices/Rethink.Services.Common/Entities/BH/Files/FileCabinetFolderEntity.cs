using Rethink.Services.Common.Entities.Base;
using System;
using System.Collections.Generic;

namespace RethinkAutism.Data.Entities.Files
{
    public class FileCabinetFoldersEntity : BasePersistEntity, IAuditedEntity
    {
        public int FileCabinetId { get; set; }
        public string FolderName { get; set; }
        public string FolderAddedBy { get; set; }
        public string FolderPath { get; set; }
        public bool? SharedFolder { get; set; }
        public bool? IsSystemFolder { get; set; }
        public string FolderDescription { get; set; }
        public int? Order { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual FileCabinetEntity FileCabinet { get; set; }
        public virtual IEnumerable<SharedFolderAccessEntity> SharedFolderAccesses { get; set; }
    }

}

