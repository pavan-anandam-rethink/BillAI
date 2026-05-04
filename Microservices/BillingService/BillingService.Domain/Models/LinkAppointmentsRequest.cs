using System.Collections.Generic;

namespace BillingService.Domain.Models
{
    public class LinkAppointmentsRequest : UserInfo
    {
        public int ClaimId { get; set; }
        public List<int> AppointmentIds { get; set; }
    }
}