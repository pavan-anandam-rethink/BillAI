using System;

namespace BillingService.Domain.Models
{
    public class ServiceLineAppointmentModel
    {
        public int Id { get; set; }
        public string AppointmentDescription { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime EndTime { get; set; }
        public string ClientName { get; set; }
        public string Location { get; set; }
    }
}
