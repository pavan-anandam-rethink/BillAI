namespace BillingService.Domain.Models.Claims
{
    public class ClaimSearchModel : UserInfo
    {
        public string SearchString { get; set; }
        public int PaymentId { get; set; }
    }
}
