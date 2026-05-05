namespace BillingService.Domain.Models.Claims
{
    public class UpdateClaimRequestModel : UserInfo
    {
        public int ClaimId { get; set; }
        public int ClaimStatusId { get; set; }
    }
}
