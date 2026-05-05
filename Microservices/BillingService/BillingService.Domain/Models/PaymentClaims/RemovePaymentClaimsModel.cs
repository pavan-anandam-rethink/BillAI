namespace BillingService.Domain.Models.PaymentClaims
{
    public class RemovePaymentClaimsModel : UserInfo
    {
        public int PaymentId { get; set; }
        public int[] PaymentClaimsIds { get; set; }
    }
}
