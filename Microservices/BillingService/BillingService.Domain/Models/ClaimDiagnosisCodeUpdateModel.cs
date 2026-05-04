using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class ClaimDiagnosisCodeUpdateModel
    {
        public int DiagnosisId { get; set; }
        public string DiagnosisCode { get; set; }
        public int Order { get; set; }
        public bool IncludeOnClaims { get; set; }
        public string Description { get; set; }
    }
}