using BillingService.Domain.Scheduling.Interfaces;
using BillingService.Domain.Scheduling.Models;
using System;
using System.Linq;

namespace BillingService.Domain.Scheduling.Strategies;

public class WeeklySchedule : IScheduleClaimFrequency
{
    public DateTime GetNextExecutionUtc(UnProcessedApointmentSchedule model)
    {
        var selectedDays = model.SelectedDays
            .Split(',')
            .Select(x => (DayOfWeek)Enum.Parse(typeof(DayOfWeek), x.Trim(), true))
            .ToList();

        var current = model.CurrentUtc;
        var currentDay = current.DayOfWeek;

        var orderedDays = selectedDays
                          .Select(day => new{Day = day,Diff = ((int)day - (int)currentDay + 7) % 7})
                          .OrderBy(x => x.Diff)
                          .ToList();

        foreach (var item in orderedDays)
        {
            var candidate = current.Date.AddDays(item.Diff) + model.UtcExecutionTime;

            if (candidate > current)
                return candidate;
        }

        // fallback next week (earliest day)
        var first = orderedDays.First();
        return current.Date.AddDays(first.Diff + 7) + model.UtcExecutionTime;
    }
}