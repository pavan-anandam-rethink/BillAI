using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentClaimServiceLine
{
    public class AddOrEditAdjustmentModel : UserInfo
    {
        public int ClaimId { get; set; }
        public int ServiceLineId { get; set; }
        public List<AdjustmentDetailsModel> AdjustmentDetails { get; set; }
    }
    public class AdjustmentDetailsModel
    {
        public int? AdjustmentId { get; set; }
        public decimal Amount { get; set; }
        public bool? isPositive { get; set; }
        public string GroupCode { get; set; }
        public string ReasonCode { get; set; }
    }

    public class AddOrEditAdjustmentModelForBulkPosting : AddOrEditAdjustmentModel
    {
        public decimal? AllowedAmount { get; set; }
        public decimal PaymentAmount { get; set; }
    }
}
