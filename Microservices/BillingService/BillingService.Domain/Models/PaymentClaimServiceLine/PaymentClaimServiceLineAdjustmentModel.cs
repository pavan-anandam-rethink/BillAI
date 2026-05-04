using System;

namespace BillingService.Domain.Models.PaymentClaimServiceLineAdjustment
{
    public class PaymentClaimServiceLineAdjustmentModel
    {
        public int Id { get; set; }
        public int? serviceLineId { get; set; }
        public string PaymentIdentifier { get; set; }
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public bool? isPositive { get; set; }
        public string GroupCode { get; set; }
        public string ReasonCode { get; set; }
        public string Description { get; set; }
        public DateTime? PostDate { get; set; }
    }
}