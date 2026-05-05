using System;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class UpdatePaymentSummary
    {
        public int Id { get; set; }
        public DateTime? PostDate { get; set; }
        public int PaymentMethodId { get; set; }
        public DateTime? DepositDate { get; set; }
        public decimal PaymentAmount { get; set; }
    }
}