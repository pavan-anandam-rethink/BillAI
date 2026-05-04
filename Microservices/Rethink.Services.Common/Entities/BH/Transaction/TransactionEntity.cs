using System;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Member;

namespace Rethink.Services.Common.Entities.BH.Transaction
{
    public class TransactionEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("referenceId")]
        public int ReferenceId { get; set; }
        [Column("referenceTypeId")]
        public int ReferenceTypeId { get; set; }
        [Column("tableName")]
        public string TableName { get; set; }
        [Column("hcTransactionTypeId")]
        public int TypeId { get; set; }
        [Column("oldValues")]
        public string OldValues { get; set; }
        [Column("newValues")]
        public string NewValues { get; set; }
        [Column("transactionOn")]
        public DateTime TransactionOn { get; set; }
        [Column("transactionBy")]
        public int TransactionBy { get; set; }

        [Column("action")]
        public string Action { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }

        public virtual MemberEntity Member { get; set; }

    }
}
