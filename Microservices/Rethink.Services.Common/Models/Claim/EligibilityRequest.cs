using System;
using System.Net;

namespace Rethink.Services.Common.Models.Claim
{
    public class EligibilityRequest
    {
        public int? FunderId { get; set; }
        public int? AccountId { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class EligibilityResponse
    {
        public int? FunderId { get; set; }
        public int? AccountId { get; set; }
        public DateTime? EffectiveStartDate { get; set; }
        public DateTime? EffectiveEndDate { get; set; }
        public string CoverageStatus { get; set; }
        public DateTime? SubscriberStartDate { get; set; }
        public DateTime? SubscriberEndDate { get; set; }
        public DateTime? PlanStartDate { get; set; }
        public DateTime? PlanEndDate { get; set; }        
        public string FailureResponse { get; init; }
    }
}
