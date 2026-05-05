using BillingService.Domain.Models.PaymentPosting;

namespace BillingService.Domain.Models.Claims
{
    public class ClaimValidationModel : IdWithUserInfo
    {
        public bool IsSecondary { get; set; } = false;
        public int? SecondaryFunderId { get; set; }
    }
}
