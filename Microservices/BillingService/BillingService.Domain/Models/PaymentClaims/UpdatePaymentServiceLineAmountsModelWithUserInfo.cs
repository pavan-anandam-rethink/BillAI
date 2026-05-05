using BillingService.Domain.Models.PaymentClaimServiceLine;
using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentClaims
{
    public class UpdatePaymentServiceLineAmountsModelWithUserInfo : UserInfo
    {
        public int ServiceLineId { get; set; }
        public decimal? AllowedAmount { get; set; }
        public decimal PaymentAmount { get; set; }
        public bool IsManual { get; set; }
    }
}