namespace BillingService.Domain.Models.BillingSettings;

public class FunderSettingsRequest
{
    public int ScheduleType { get; set; }

    public string ScheduleTime { get; set; }

    public int? ScheduleTimeZone { get; set; }

    public string WeeklyDays { get; set; }

    public int? MonthlyFrequency { get; set; }

    public bool? CombineChargesForSameClient { get; set; }
    public int ClaimFilingIndicatorId { get; set; }
    public bool IncludeTaxonomyCode { get; set; }
}