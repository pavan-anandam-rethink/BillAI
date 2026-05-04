using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class DeleteAttachmentsModelWithUserInfo : UserInfo
    {
        public List<int> Ids { get; set; }
    }
}