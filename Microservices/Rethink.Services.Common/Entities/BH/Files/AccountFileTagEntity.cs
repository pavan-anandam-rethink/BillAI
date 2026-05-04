using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Member;
using RethinkAutism.Data.Entities.Members;
using System;
using System.Collections.Generic;

namespace RethinkAutism.Data.Entities.Files
{
    public class AccountFileTagEntity : BasePersistEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int AccountId { get; set; }

        public AccountInfoEntity Account { get; set; }

        public ICollection<AccountTagToFileEntity> TagFiles { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int ModifiedBy { get; set; }
    }
}
