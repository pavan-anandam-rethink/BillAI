using Rethink.Services.Common.Entities.Base;
using System;
using System.Collections.Generic;

namespace RethinkAutism.Data.Entities.Files
{
    public class FileCabinetFilesEntity : BasePersistEntity, IAuditedEntity
    {

        public int FileCabinetId { get; set; }

        public string FileName { get; set; }

        public double FileSize { get; set; }

        public string HollowFilePath { get; set; }

        public string FileMimeType { get; set; }

        public string FileAddedBy { get; set; }

        public string FilePath { get; set; }

        public int? FileCabinetFolderId { get; set; }

        public int? ReferenceId { get; set; }

        public string OriginalFileName { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }

        public FileStatus? Status { get; set; }

        public int? SessionNoteResponseId { get; set; }

        public virtual FileCabinetEntity FileCabinet { get; set; }

        public virtual FileCabinetFoldersEntity FileCabinetFolder { get; set; }

        public ICollection<AccountTagToFileEntity> AccountFileTags { get; set; }
    }
}
