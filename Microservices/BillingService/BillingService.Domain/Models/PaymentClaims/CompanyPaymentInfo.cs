using BillingService.Domain.Models.PaymentPosting;

namespace BillingService.Domain.Models.PaymentClaims
{
    public class GetClientPrintDataRequest : IdWithUserInfo
    {
        public int PatientId { get; set; }
        public int ClaimId { get; set; }
    }
}
