using System.Collections.Generic;

namespace BillingService.Domain.Models
{
    public class MemberViewSettingWithUserInfo : UserInfo
    {
        public List<string> SelectedColumns { get; set; }
    }
}
