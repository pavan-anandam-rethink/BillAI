namespace BillingService.Domain.Models
{
    public class RemoveBillingClaimDetailsModel
    {
        public int ChargeId { get; set; }
        public int AccountId { get; set; }
        public int MemberId { get; set; }
    }
}
