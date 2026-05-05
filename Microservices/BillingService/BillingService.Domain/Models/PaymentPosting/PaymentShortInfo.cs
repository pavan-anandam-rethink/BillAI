namespace BillingService.Domain.Models.PaymentPosting
{
    public class PaymentShortInfo
    {
        public int Id { get; set; }
        public string PaymentIdentifier { get; set; }
        public string ReconcileStatus { get; set; }
        public int ErrorsCount { get; set; }
        public bool IsManual { get; set; }
        public bool IsPatientType { get; set; }
        public bool IsOtherType { get; set; }
        public bool IsInsuranceType { get; set; }
    }
}