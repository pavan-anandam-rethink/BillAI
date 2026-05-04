namespace BillingService.Domain.Models.PaymentClaims
{
    public class CreateEraClaimsModel : UserInfo
    {
        public int PaymentId { get; set; }
        public int[] ClaimsIds { get; set; }
    }
}
