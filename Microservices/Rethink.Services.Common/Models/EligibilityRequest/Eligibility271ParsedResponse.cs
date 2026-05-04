using System;
using System.Net;

namespace Rethink.Services.Common.Models.EligibilityRequest
{
    public class Eligibility271ParsedResponse
    {
        public string CoverageStatus { get; set; }
        public DateTime? EffectiveStartDate { get; set; }
        public DateTime? EffectiveEndDate { get; set; }
        public DateTime? SubscriberStartDate { get; set; }
        public DateTime? SubscriberEndDate { get; set; }
        public DateTime? PlanStartDate { get; set; }
        public DateTime? PlanEndDate { get; set; }
        public DateTime? PendingPlanDates { get; set; }
        public bool IsSuccess { get; init; }
        public string X12Response { get; init; }
        public string FailureResponse { get; init; }
    }
}
