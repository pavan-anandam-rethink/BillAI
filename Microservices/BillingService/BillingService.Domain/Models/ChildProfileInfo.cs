using System;

namespace BillingService.Domain.Models
{
    public class ChildProfileInfo
    {
        public int PatientId { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        public string PatientName { get; set; }
        public string Age { get; set; }
        public string Uci { get; set; }
        public string ServiceIntensity { get; set; }
        public string PrimaryPolicy { get; set; }
        public string SecondaryPolicy { get; set; }
        public string Location { get; set; }
    }
}
