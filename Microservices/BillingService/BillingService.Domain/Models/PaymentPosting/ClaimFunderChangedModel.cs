using System;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class ClaimFunderChangedModel : IdWithUserInfo
    {
        public int ClientFunderId { get; set; }
        public DateTime FunderModifiedDate { get; set; }
    }
}
