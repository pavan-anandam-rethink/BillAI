using Rethink.Services.Common.Enums.Billing;

namespace BillingService.Domain.Models.Claims
{
    public class ClaimFilterGetModel : UserInfo
    {
        public string SearchValue { get; set; }
        public ClaimListingTab Tab { get; set; }
    }
}
