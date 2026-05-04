using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class FunderSearchModelWithUserInfo : UserInfo
    {
        public string FunderName { get; set; }
        public List<string> FundersWithNoId { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
    }
}