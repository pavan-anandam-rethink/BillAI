namespace BillingService.Domain.Models
{
    public class ChargeIdWithUserInfo : UserInfo
    {
        public int ChargeId { get; set; }
    }
}