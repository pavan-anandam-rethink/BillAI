using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Enums.Billing;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Claim.History
{
    public class ClaimHistoryEntity : BasePersistEntity, IAuditedEntity
    {

        public int ClaimId { get; set; }

        [Column("claimActionId")]
        public ClaimAction ClaimAction { get; set; }

        [Column("claimActionTypeId")]
        public ClaimActionMode Mode { get; set; }

        [Column("claimHistoryActionId")]
        public ClaimHistoryAction ClaimHistoryAction { get; set; }

        [Column("claimHistoryFieldId")]
        public ClaimHistoryField? ClaimHistoryField { get; set; }

        [Column("claimVersionId")]
        public int? ClaimVersionId { get; set; }
        public DateTime ActionDate { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public string? RethinkUser { get; set; } = "N/A";

        public virtual ClaimEntity Claim { get; set; }
        public virtual ClaimVersionEntity ClaimVersion { get; set; }
    }
}