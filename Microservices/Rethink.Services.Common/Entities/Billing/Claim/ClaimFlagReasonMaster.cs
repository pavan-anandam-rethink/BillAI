using System;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class ClaimFlagReasonMaster : BasePersistEntity, IAuditedEntity
    {
        public string ReasonName { get; set; } = null!;
        public string? ReasonDescription { get; set; }
        public int AccountInfoId { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
    }
}
