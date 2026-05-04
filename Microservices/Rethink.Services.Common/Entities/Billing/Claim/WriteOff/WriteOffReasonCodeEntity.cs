using Rethink.Services.Common.Entities.Base;
using System;
using System.Collections.Generic;

namespace Rethink.Services.Common.Entities.Billing.Claim.WriteOff
{
    public class WriteOffReasonCodeEntity : BasePersistEntity, IAuditedEntity
    {
        public string Description { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public virtual ICollection<ClaimChargeEntryWriteOffEntity> ClaimChargeEntryWriteOffs { get; set; }

    }
}
