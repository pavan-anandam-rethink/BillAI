using Rethink.Services.Common.Enums.Billing;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class PersonSearchModel : UserInfo
    {
        public string PersonName { get; set; }
        public ClaimListingTab Tab { get; set; }
    }
}