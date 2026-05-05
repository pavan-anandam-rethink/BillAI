namespace BillingService.Domain.Models.Claims
{
    public class ClaimsVoidModel
    {
        public int[] ClaimIds { get; set; }
        public bool SubmitToClearinghouse { get; set; } = false;
        public string Note { get; set; }       
        public string claimNote { get; set; }
    }
}
