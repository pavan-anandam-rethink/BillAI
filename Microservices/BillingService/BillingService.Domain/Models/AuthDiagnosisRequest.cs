using System;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class AuthDiagnosisRequest : UserInfo
    {
        public int ChildProfileId { get; set; }
        public int ServiceLineId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IncludeInactive { get; set; }
    }
}
