using Rethink.Services.Common.Entities.Base;
using System;

namespace Rethink.Services.Common.Entities.Billing
{
    public class FunderSettingsEntity : BasePersistEntity
    {
        public override int Id { get; set; }

        public int AccountInfoId { get; set; }

        public int FunderId { get; set; }
        public string FunderName { get; set; } = null!;

        public int ClaimFilingIndicatorId { get; set; }
        public bool IncludeTaxonomyCode { get; set; }
        public TimeSpan? ScheduleTime { get; set; }
        public string WeeklyDays { get; set; }
        public DateTime? NextRunDate { get; set; }
        public bool? CombineChargesForSameClient { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int ScheduleType { get; set; }
        public int? ScheduleTimeZone { get; set; }
        public int? MonthlyFrequency { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }

        public ClaimFilingIndicatorEntity ClaimFilingIndicator { get; set; } 
    }
}
