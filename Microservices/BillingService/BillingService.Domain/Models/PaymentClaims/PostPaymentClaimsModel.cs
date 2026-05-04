using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentClaims
{
    public class PostPaymentClaimsModel
    {
        public int PaymentId { get; set; }
        public List<PostPaymentClaimLinesModel> SelectedClaimLines { get; set; }
    }
}
