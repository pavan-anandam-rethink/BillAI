using System;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Models.Claims
{
    [ExcludeFromCodeCoverage]
    public class ClaimDropdownModel
    {
        public int Id { get; set; }
        public string ClaimIdentifier { get; set; }
        public int ChildProfileId { get; set; }
        public string PatientName { get; set; }
        public string PatientFirstName { get; set; }
        public string PatientMiddleName { get; set; }
        public string PatientLastName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool AlreadyCreated { get; set; }
    }
}
