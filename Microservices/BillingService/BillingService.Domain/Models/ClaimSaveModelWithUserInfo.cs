using BillingService.Domain.Models.PaymentPosting;
namespace BillingService.Domain.Models
{
    public class ClaimSaveModelWithUserInfo : UserInfo
    {
        public ClaimSaveModel Claim { get; set; }
        public BillingProviderRequest? BillingProviderRequest { get; set; }
    }
}
