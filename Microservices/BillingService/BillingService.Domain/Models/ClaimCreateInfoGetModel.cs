namespace BillingService.Domain.Models
{
    public class ClaimCreateInfoGetModel : UserInfo
    {
        public int ClientId { get; set; }
        public int FunderId { get; set; }
        public int ServiceId { get; set; }
    }
}
