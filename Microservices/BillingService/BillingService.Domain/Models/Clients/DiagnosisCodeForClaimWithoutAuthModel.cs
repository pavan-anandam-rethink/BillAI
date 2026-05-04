using System;

namespace BillingService.Domain.Models.Clients
{
    public class DiagnosisCodeForClaimWithoutAuthModel
    {
        public int DiagnosisId { get; set; }
        public string DiagnosisCode { get; set; }
        public string DiagnosisDescription { get; set; }
        public string DiagnosisFullDescription { get; set; }
        public int Order { get; set; }
        public bool IncludeOnClaims { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
