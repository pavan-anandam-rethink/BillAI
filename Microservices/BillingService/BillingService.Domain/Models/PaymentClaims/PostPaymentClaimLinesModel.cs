using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentClaims
{
    public class PostPaymentClaimLinesModel
    {
        public int ClaimId { get; set; }
        public bool IsClaimSelected { get; set; }
        public List<PostPaymentLineModel> SelectedLines { get; set; }
    }
}
