namespace BillingService.Domain.Models
{
    public class FunderServiceLineRequestModel : UserInfo
    {
        public int ClientId { get; set; }
        public int FunderId { get; set; }
        public int Id { get; set; }

    }
}
