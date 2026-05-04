using Rethink.Services.Common.Entities.Base;
using System;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class StateEntity : BasePersistEntity, IAuditedEntity
    {
        public string StateName { get; set; }

        public string StateCode { get; set; }

        public int? UtcOffSet { get; set; }

        public int? UtcDSTOffSet { get; set; }

        public bool SupportsSandata { get; set; }

        public DateTime DateCreated { get; set; }

        public int CreatedBy { get; set; }

        public DateTime? DateLastModified { get; set; }

        public int? ModifiedBy { get; set; }

        public DateTime? DateDeleted { get; set; }

        public int? DeletedBy { get; set; }
    }
}
