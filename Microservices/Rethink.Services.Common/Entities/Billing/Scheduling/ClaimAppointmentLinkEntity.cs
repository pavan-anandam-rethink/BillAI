using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Scheduling
{
    public class ClaimAppointmentLinkEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("hcClaimId")]
        public int ClaimId { get; set; }
        [Column("hcAppointmentId")]
        public int AppointmentId { get; set; }
        [Column("hcClaimAppointmentLinkChargeEntryId")]
        public int? ClaimAppointmentLinkChargeEntryId { get; set; }
        [Column("hcClaimChargeEntriesId")]
        public int? ClaimChargeEntriesId { get; set; }
        public int? AccountInfoId { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual ClaimEntity Claim { get; set; }
        [NotMapped]
        public virtual AppointmentRethinkModel Appointment { get; set; }

        public virtual ClaimAppointmentLinkChargeEntry ClaimAppointmentLinkChargeEntry { get; set; }

        public virtual AppointmentClaimProcessingErrorEntity AppointmentClaimProcessingError { get; set; }

        //public int EntityTypeId => 7;
        //[NotMapped]
        //public int? TypeId { get; set; }
        //public List<string> PropertiesToExclude => new List<string> 
        //    { "CreatedBy", "ModifiedBy", "DateCreated", "DateLastModified" };
    }
}