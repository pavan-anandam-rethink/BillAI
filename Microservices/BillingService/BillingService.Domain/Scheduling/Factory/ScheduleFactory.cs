using BillingService.Domain.Scheduling.Interfaces;
using BillingService.Domain.Scheduling.Strategies;
using Rethink.Services.Common.Enums.Billing;
using System;

namespace BillingService.Domain.Scheduling.Factory;

public static class ScheduleFactory
{
    public static IScheduleClaimFrequency Get(ClaimCreationFrequency frequency)
    {
        return frequency switch
        {
            ClaimCreationFrequency.Daily => new DailySchedule(),
            ClaimCreationFrequency.Weekly => new WeeklySchedule(),
            ClaimCreationFrequency.Monthly => new MonthlySchedule(),
            _ => throw new NotSupportedException()
        };
    }
}