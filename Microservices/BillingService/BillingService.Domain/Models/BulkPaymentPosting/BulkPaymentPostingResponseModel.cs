using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Models.BulkPaymentPosting
{
    [ExcludeFromCodeCoverage]
    public class BulkPaymentPostingResponseModel : UserInfo
    {
        public string ResponseMessage { get; set; }
        public int[] FailedPayments { get; set; }
    }


}
