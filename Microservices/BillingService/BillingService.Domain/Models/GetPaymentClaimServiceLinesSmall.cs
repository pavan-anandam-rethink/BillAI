using System;

namespace BillingService.Domain.Models
{
    public class GetPaymentClaimServiceLinesSmall
    {
        public int Id { get; set; }
        public int PaymentId { get; set; }
        public string PaymentIdentifier { get; set; }
        public decimal AllowedAmount { get; set; }
        public decimal AllowedAmountOrig { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PaidAmountOrig { get; set; }
        public DateTime? DateLastModified { get; set; }
        public string PaymentType { get; set; }
    }
}
