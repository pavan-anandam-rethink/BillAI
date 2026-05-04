using System;

namespace BillingService.Domain.Models.PaymentClaimServiceLineAdjustment
{
    public class GetPaymentIdModel
    {
        public int ServiceLineId { get; set; }
        public string PaymentIdentifier { get; set; }
        public DateTime? PostDate { get; set; }
    }
}
