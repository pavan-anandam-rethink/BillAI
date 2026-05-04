namespace BillingService.Domain.Models.PaymentClaims
{
    public class CreatePatientClaimsModel : UserInfo
    {
        public int PaymentId { get; set; }
        public int[] PatientIds { get; set; }
        public decimal[] UnAllocatedAmount { get; set; }
        public string[]? Notes { get; set; }
    }

    public class AddPatientResponseModel
    {
        public int patientId { get; set; }
        public string patientName { get; set; }
        public bool isAttached { get; set; }
    }

    public class GetInvoicePDFRequestModel
    {
        public int AccountId { get; set; }
        public int ClientId { get; set; }
        public string InvoiceNo { get; set; }
    }
}
