using Rethink.Services.Common.Entities.Base;
using System;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class Eligibility271ResponseEntity : BasePersistEntity
    {
       
        public long Eligibility271ResponseId { get; set; }
        public Guid TransactionControlNumber { get; set; }
        public int? FunderId { get; set; }
        public int? AccountId { get; set; }
        public DateTime? EffectiveStartDate { get; set; }
        public DateTime? EffectiveEndDate { get; set; }
        public string CoverageStatus { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }
        public DateTime? SubscriberStartDate { get; set; }
        public DateTime? SubscriberEndDate { get; set; }
        public DateTime? PlanStartDate { get; set; }
        public DateTime? PlanEndDate { get; set; }
        public string FailureResponse { get; set; }
    }
}
