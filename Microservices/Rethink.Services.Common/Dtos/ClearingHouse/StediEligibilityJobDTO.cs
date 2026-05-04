using System;

namespace Rethink.Services.Common.Dtos.ClearingHouse
{
    public class StediEligibilityJobDTO
    {
        public string Edi270Request { get; set; } = default!;
        public int? FunderId { get; set; }
        public int? AccountId { get; set; }
        public int? MemberId { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
        public DateTime? PlanStartDate { get; set; }
        public DateTime? PlanEndDate { get; set; }
    }
}
