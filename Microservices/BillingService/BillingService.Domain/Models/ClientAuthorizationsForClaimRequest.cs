namespace BillingService.Domain.Models
{
    public class ClientAuthorizationsForClaimRequest : UserInfo
    {
        public int ChildProfileId { get; set; }
        public int FunderId { get; set; }
        public int ClientFunderServiceLineId { get; set; }
    }
}
