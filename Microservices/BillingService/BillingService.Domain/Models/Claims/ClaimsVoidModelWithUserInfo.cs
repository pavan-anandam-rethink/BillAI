namespace BillingService.Domain.Models.Claims
{
    public class ClaimsVoidModelWithUserInfo : UserInfo
    {
        //public int ClearingHouseId { get; set; }
        public ClaimsVoidModel ClaimsToVoid { get; set; }
    }
}
