using Rethink.Services.Common.Entities.Base;
using System;

namespace RethinkAutism.Data.Entities.Files
{
    public class AccountTagToFileEntity : BasePersistEntity
    {

        public int FileId { get; set; }

        public int AccountFileTagId { get; set; }

        public FileCabinetFilesEntity File { get; set; }

        public AccountFileTagEntity Tag { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int ModifiedBy { get; set; }
    }
}
