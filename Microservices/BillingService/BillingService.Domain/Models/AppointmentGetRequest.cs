using System;

namespace BillingService.Domain.Models
{
    public class AppointmentGetRequest
    {
        public int ClaimId { get; set; }
        public int ClientId { get; set; }
        public int AccountInfoId { get; set; }
        public int MemberId { get; set; }
        public int? LocationId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}