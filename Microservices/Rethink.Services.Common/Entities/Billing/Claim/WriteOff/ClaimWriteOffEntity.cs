using Rethink.Services.Common.Entities.Base;
using System;
using System.Collections.Generic;

namespace Rethink.Services.Common.Entities.Billing.Claim.WriteOff
{
    public class ClaimWriteOffEntity : BasePersistEntity, IAuditedEntity
    {
        public int ClaimId { get; set; }
        public int? WriteOffApplicationId { get; set; }
        public int WriteOffActionId { get; set; }
        public decimal? PercentageOrAmount { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public virtual ClaimEntity Claim { get; set; }
        public virtual WriteOffActionEntity WriteOffAction { get; set; }
        public virtual WriteOffApplicationEntity WriteOffApplication { get; set; }
        public virtual ICollection<ClaimChargeEntryWriteOffEntity> ClaimChargeEntryWriteOffs { get; set; }

    }
}
