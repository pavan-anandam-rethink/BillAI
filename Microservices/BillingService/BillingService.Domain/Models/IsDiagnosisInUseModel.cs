namespace BillingService.Domain.Models
{
    public class IsDiagnosisInUseModel
    {
        public int ClientId { get; set; }
        public int DiagnosisMapId { get; set; }
        public int DiagnosisCodeId { get; set; }
    }
}
