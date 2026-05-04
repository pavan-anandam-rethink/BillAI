namespace BillingService.Domain.Models.Claims
{
    public class SecondaryFunderDetailsModel
    {
        public int ClaimId { get; set; }
        public int? SecondaryFunderId { get; set; }
        public string ControlNumber { get; set; }
    }
}
