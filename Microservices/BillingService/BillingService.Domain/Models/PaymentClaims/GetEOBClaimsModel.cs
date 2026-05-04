using System;
using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentClaims
{
    public class GetEOBClaimsModel
    {
        public int PaymentId { get; set; }
        public List<int> Claims { get; set; }
        public DateTime CurrentUserDateTime { get; set; }
        public bool ShowErrors { get; set; }
    }
}