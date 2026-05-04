using BillingService.Domain.Scheduling.Interfaces;
using BillingService.Domain.Scheduling.Models;
using System;

namespace BillingService.Domain.Scheduling.Strategies;

public class DailySchedule : IScheduleClaimFrequency
{
    public DateTime GetNextExecutionUtc(UnProcessedApointmentSchedule unProcessedApointmentSchedule)
    {
        var today = unProcessedApointmentSchedule.CurrentUtc.Date + unProcessedApointmentSchedule.UtcExecutionTime;

        if (today > unProcessedApointmentSchedule.CurrentUtc)
            return today;

        return unProcessedApointmentSchedule.CurrentUtc.Date.AddDays(1) + unProcessedApointmentSchedule.UtcExecutionTime;
    }
}