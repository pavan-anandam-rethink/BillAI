using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class PaymentClaimErrorModel
    {
        public int Id { get; set; }
        public string PatientName { get; set; }
        public int PatientId { get; set; }
        public string ClaimIdentifier { get; set; }
        public decimal ExpectedAmount { get; set; }
        public decimal AllowedAmount { get; set; }
        public decimal Balance { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PaymentClaimErrorsResponseModel
    {
        public List<PaymentClaimErrorModel> Data { get; set; }
        public int TotalCount { get; set; }
    }
}