namespace BillingService.Domain.Models
{
    public class ClaimDiagnosisCodeModel
    {
        public int DiagnosisId { get; set; }
        public string DiagnosisCode { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public bool IncludeOnClaims { get; set; }
    }
}