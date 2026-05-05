using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class HideProcessingInfoModelWithUserInfo : UserInfo
    {
        public List<int> PaymentIds { get; set; }
    }
}