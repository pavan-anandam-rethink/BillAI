using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.DataObjects.Billing
{
    [ExcludeFromCodeCoverage]
    public class ClaimsCountModel
    {
        public int PendingReviewTotalCount { get; set; }
        public int ReadyToBillTotalCount { get; set; }
        public int BillingPendingTotalCount { get; set; }
        public int ClosedTotalCount { get; set; }
        public int RejectedTotalCount { get; set; }
        public int DeniedTotalCount { get; set; }
        public int FlaggedTotalCount { get; set; }
    }
}