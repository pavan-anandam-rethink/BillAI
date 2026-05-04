using BillingService.Domain.Models.BillingSettings;
using Rethink.Services.Common.Entities.Billing;
using System;

namespace BillingService.Domain.Mapper;

public static class FunderSettingsMapper
{
    public static FunderSettingsEntity ToFunderSettingEntity(this FunderSettingRequest model)
    {
        return new FunderSettingsEntity
        {
            AccountInfoId = model.AccountInfoId,
            FunderId = model.FunderId,
            FunderName = model.FunderName,
            ClaimFilingIndicatorId = model.ClaimFilingIndicatorId,
            IncludeTaxonomyCode = model.IncludeTaxonomyCode,
            DateCreated = DateTime.UtcNow,
            ScheduleType = model.Data.ScheduleType,
            ScheduleTime = string.IsNullOrWhiteSpace(model.Data.ScheduleTime)
                         ? null
                         : TimeSpan.Parse(model.Data.ScheduleTime),
            ScheduleTimeZone = model.Data.ScheduleTimeZone,
            WeeklyDays = model.Data.WeeklyDays,
            MonthlyFrequency = model.Data.MonthlyFrequency,
            CombineChargesForSameClient = model.Data.CombineChargesForSameClient ?? false,
            CreatedBy = model.ChangedBy
        };
    }
}