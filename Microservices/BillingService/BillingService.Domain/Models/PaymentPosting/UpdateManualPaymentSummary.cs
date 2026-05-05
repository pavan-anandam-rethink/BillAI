using System;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class UpdateManualPaymentSummary : UserInfo
    {
        public int Id { get; set; }
        public DateTime? PostDate { get; set; }
        public int PaymentMethodId { get; set; }
        public DateTime? DepositDate { get; set; }
        public string ReferenceNumber { get; set; }
        public decimal PaymentAmount { get; set; }
    }
}
