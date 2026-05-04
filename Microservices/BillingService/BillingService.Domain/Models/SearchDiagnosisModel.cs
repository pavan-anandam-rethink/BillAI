namespace BillingService.Domain.Models
{
    public class SearchDiagnosisModel : UserInfo
    {
        public int? DiagnosisTypeId { get; set; }
        public string SearchTerm { get; set; }
        public int? ClientId { get; set; }
        public bool AddCustom { get; set; }
    }
}
