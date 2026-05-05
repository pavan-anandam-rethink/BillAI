using System;

namespace Rethink.Services.Common.Entities.Billing.Claim.History;

public class FunderSettingsAuditEntity
{
    public int AccountInfoId { get; set; }

    public int FunderId { get; set; }

    public string ClaimFilingIndicatorDescr { get; set; }

    public string IncludeTaxonomyCode { get; set; }

    public string ScheduleType { get; set; }

    public string ScheduleTime { get; set; }

    public string ScheduleTimeZone { get; set; }

    public string WeeklyDays { get; set; }

    public string MonthlyFrequency { get; set; }

    public string CombineChargesForSameClient { get; set; }
}