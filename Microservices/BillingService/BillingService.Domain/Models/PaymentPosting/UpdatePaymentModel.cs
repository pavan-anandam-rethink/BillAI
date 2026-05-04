namespace BillingService.Domain.Models.PaymentPosting
{
    public class UpdatePaymentModel : UserInfo
    {
        public int[] PaymentId { get; set; }
    }

    public class ClaimPaymentUpdateModel : UpdatePaymentModel
    {
        public int ClaimId { get; set; }
    }
}