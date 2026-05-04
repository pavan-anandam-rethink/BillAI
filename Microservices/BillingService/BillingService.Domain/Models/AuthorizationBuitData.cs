namespace BillingService.Domain.Models
{
    public class AuthorizationBuitData
    {
        public string BillingCode { get; set; }
        public string BillingCodeDescription { get; set; }
        public decimal? Units { get; set; }
        public string ServiceLineIdentifier { get; set; }
        public int? ServiceLineIndex { get; set; }
    }
}
