using Rethink.Services.Common.Entities.Base;
using System;

namespace Rethink.Services.Common.Entities.Billing.Claim.WriteOff
{
    public class ClaimChargeEntryWriteOffEntity : BasePersistEntity, IAuditedEntity
    {
        public int ClaimChargeEntryId { get; set; }
        public int ClaimWriteOffId { get; set; }
        public int WriteOffReasonCodeId { get; set; }
        public int WriteOffReasonCodeIdOrig { get; set; }
        public decimal? WriteOffAmount { get; set; }
        public decimal? WriteOffAmountOrig { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public virtual ClaimWriteOffEntity ClaimWriteOff { get; set; }
        public virtual WriteOffReasonCodeEntity WriteOffReasonCode { get; set; }
        public virtual ClaimChargeEntryEntity ClaimChargeEntry { get; set; }

    }
}
