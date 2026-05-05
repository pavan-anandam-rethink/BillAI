using BillingService.Domain.Models.PaymentClaims;
using BillingService.Domain.Models.PaymentPosting;
using System;
using System.Collections.Generic;

namespace BillingService.Domain.Templates.ViewModels
{
    public class PaymentClaimEOBViewModel
    {
        public EOBPaymentInfo PaymentEOB { get; set; }
        public IEnumerable<ClaimEOBInfoModel> ClaimEOBInfo { get; set; }
        public DateTime CurrentTime { get; set; }
        public bool ShowErrors { get; set; }
    }
}
