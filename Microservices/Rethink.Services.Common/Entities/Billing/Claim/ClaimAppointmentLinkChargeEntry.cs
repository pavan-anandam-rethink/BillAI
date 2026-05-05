using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class ClaimAppointmentLinkChargeEntry : BasePersistEntity, IAuditedEntity
    {
        [Column("hcClaimChargeEntryEntityId")]
        public int ClaimChargeEntryEntityId { get; set; }
        public string NpiNumber { get; set; }

        public bool? IsSecondBillingCode { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public ClaimChargeEntryEntity ClaimChargeEntry { get; set; }

        public virtual ICollection<ClaimAppointmentLinkEntity> ClaimAppointmentLinks { get; set; }
    }
}
