using BillingService.Domain.Models.BillingSettings;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Enums.Billing;
using System;

namespace BillingService.Domain.Mapper;

public static class FunderSettingsAuditEntityMapper
{
    public static FunderSettingsAuditEntity ToFunderSettingAuditEntity(this FunderSettingRequest model, string timezoneDisplayName, string claimFilingIndicatorDescription)
    {

        return new FunderSettingsAuditEntity
        {
            AccountInfoId = model.AccountInfoId,
            FunderId = model.FunderId,
            ClaimFilingIndicatorDescr = claimFilingIndicatorDescription,
            IncludeTaxonomyCode = model.IncludeTaxonomyCode.ToString(),
            ScheduleType = model.Data?.ScheduleType != null ? Enum.GetName(typeof(ClaimCreationFrequency), model.Data.ScheduleType) : null,
            ScheduleTime = model.Data.ScheduleTime,
            ScheduleTimeZone = timezoneDisplayName,
            WeeklyDays = model.Data.WeeklyDays, 
            MonthlyFrequency = model.Data.MonthlyFrequency != null
                             ? Enum.GetName(typeof(MonthlyFrequency), model.Data.MonthlyFrequency)
                             : null,
            CombineChargesForSameClient = model.Data.CombineChargesForSameClient.ToString(),
        };
    }

    public static FunderSettingsAuditEntity ToFunderSettingAuditEntity(this FunderSettingsEntity model, string timezoneDisplayName, string claimFilingIndicatorDescription, int? scheduleType, int? monthlyFrequency)
    {

        return new FunderSettingsAuditEntity
        {
            AccountInfoId = model.AccountInfoId,
            FunderId = model.FunderId,
            ClaimFilingIndicatorDescr = claimFilingIndicatorDescription,
            IncludeTaxonomyCode = model.IncludeTaxonomyCode.ToString(),
            ScheduleType = Enum.GetName(typeof(ClaimCreationFrequency), scheduleType),
            ScheduleTime = model.ScheduleTime?.ToString(),
            ScheduleTimeZone = timezoneDisplayName,
            WeeklyDays = model.WeeklyDays,
            MonthlyFrequency = monthlyFrequency != null ? Enum.GetName(typeof(MonthlyFrequency), monthlyFrequency): null,
            CombineChargesForSameClient = model?.CombineChargesForSameClient?.ToString(),
        };
    }
}