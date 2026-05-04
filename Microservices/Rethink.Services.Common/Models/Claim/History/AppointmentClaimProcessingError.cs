using Rethink.Services.Common.Entities.Billing.Scheduling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Models.Claim.History
{
    public class AppointmentClaimProcessingError
    {
        
            public int Id { get; set; }

            public int AppointmentId { get; set; }

            public string ErrorMessage { get; set; }

            public string CreatedBy { get; set; }

            public DateTime CreatedDate { get; set; }

            public string ModifiedBy { get; set; }

            public DateTime? ModifiedDate { get; set; }

            public DateTime? DateDeleted { get; set; }

            public ClaimAppointmentLinkEntity ClaimAppointmentLink { get; set; }
        
    }
}
