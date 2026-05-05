using BillingService.Domain.Models.BillingSettings;
using BillingService.Domain.Scheduling.Models;
using Rethink.Services.Common.Enums.Billing;
using System;

namespace BillingService.Domain.Scheduling.Mapper;

public static class UnProcessedApointmentScheduleExtension
{
    public static UnProcessedApointmentSchedule ToUnProcessedApointmentSchedule(this BillingFunderIdRequestModel source, int appointmentId, string displayName)
    {
        if (source is null)
            return null;

        return new UnProcessedApointmentSchedule
        {
            AccountInfoId = source.AccountInfoId,
            FunderId = source.FunderId,
            AppointmentId = appointmentId,
            ClaimCreationFrequency = (ClaimCreationFrequency)source.ScheduleType,
            SelectedDays = source.WeeklyDays,
            Frequency = source.MonthlyFrequency,
            ExecutionTime = source.ScheduleTime.ToHourMinute(),
            UtcExecutionTime = source.ScheduleTime.ConvertToUtcTime(displayName),
            TimeZone = displayName,
            CurrentUtc = DateTime.UtcNow
        };
    }
}