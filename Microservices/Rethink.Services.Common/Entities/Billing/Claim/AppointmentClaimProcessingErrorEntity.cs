using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class AppointmentClaimProcessingErrorEntity : BasePersistEntity, IAuditedEntity
    {
        public int Id { get; set; }

        public int ClaimAppointmentLinkId { get; set; }

        public string ErrorMessage { get; set; }

        public int CreatedBy { get; set; }  // Changed to int as per your migration

        public DateTime DateCreated { get; set; }  // Ensure this is a DateTime property

        public int? ModifiedBy { get; set; }

        public DateTime? DateLastModified { get; set; }

        public int? DeletedBy { get; set; }

        public DateTime? DateDeleted { get; set; }  // Nullable as per your migration

        // Foreign Key
        public ClaimAppointmentLinkEntity? ClaimAppointmentLink { get; set; }
    }
}
