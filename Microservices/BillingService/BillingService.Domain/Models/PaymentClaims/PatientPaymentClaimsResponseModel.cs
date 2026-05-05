using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentClaims
{
    public class PatientPaymentClaimsResponseModel
    {
        public List<PatientPaymentClaimFullModel> Data { get; set; }
        public int TotalCount { get; set; }
    }
}
