namespace BillingService.Domain.Models
{
    public class ClientFunderResponsiblePartyRequest : UserInfo
    {
        public int ChildProfileId { get; set; }
        public int ClientFunderId { get; set; }
    }
}
