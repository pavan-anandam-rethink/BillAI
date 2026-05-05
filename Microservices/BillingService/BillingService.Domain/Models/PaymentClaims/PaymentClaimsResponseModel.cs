using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentClaims
{
    public class PaymentClaimsResponseModel
    {
        public List<PaymentClaimModel> Data { get; set; }
        public int TotalCount { get; set; }
    }

    public class PaymentPaitentModel
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; }
    }
}