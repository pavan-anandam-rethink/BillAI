using BillingService.Domain.Models.PaymentPosting;

namespace BillingService.Domain.Models
{
    public class ClaimIdWithUserInfo : IdWithUserInfo
    {
        public string ClaimIdentifier { get; set; }
    }
}