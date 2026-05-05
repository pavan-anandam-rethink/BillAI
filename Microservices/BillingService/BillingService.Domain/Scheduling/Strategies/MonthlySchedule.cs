using BillingService.Domain.Scheduling.Interfaces;
using BillingService.Domain.Scheduling.Models;
using Rethink.Services.Common.Enums.Billing;
using System;

namespace BillingService.Domain.Scheduling.Strategies;

public class MonthlySchedule : IScheduleClaimFrequency
{
    public DateTime GetNextExecutionUtc(UnProcessedApointmentSchedule schedule)
    {
        var current = schedule.CurrentUtc;
        var firstDay = new DateTime(current.Year,current.Month,1,0,0, 0,DateTimeKind.Utc);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var todayExecution =current.Date + schedule.UtcExecutionTime;
        var nextMonthFirstDay = firstDay.AddMonths(1).Date;
        var nextMonthLastDay = firstDay.AddMonths(2).Date.AddDays(-1);

        // First day of month
        if (schedule.Frequency == (int)MonthlyFrequency.FirstDay)
        {
            if (current.Date == firstDay.Date && todayExecution > current)
                return todayExecution;

            return nextMonthFirstDay.Date + schedule.UtcExecutionTime;
        }

        // Last day of month
        if (schedule.Frequency == (int)MonthlyFrequency.LastDay)
        {
            if (current.Date == lastDay.Date && todayExecution > current)
                return todayExecution;
            else if(current.Date<lastDay.Date)
                return lastDay.Date + schedule.UtcExecutionTime;

            return nextMonthLastDay + schedule.UtcExecutionTime;
        }

        // Default → Any middle Day of month
        return lastDay.Date + schedule.UtcExecutionTime;
    }
}