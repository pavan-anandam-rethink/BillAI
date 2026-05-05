namespace BillingService.Domain.Models.Claims
{
    public class ClaimsRebillModelWithUserInfo : UserInfo
    {        
        public ClaimsRebillModel ClaimsToRebill { get; set; }
    }
    
}
