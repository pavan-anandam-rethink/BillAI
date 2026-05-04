using BillingService.Domain.Scheduling.Models;
using BillingService.Domain.Scheduling.Strategies;
using System;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Scheduling;

public class WeeklyScheduleTests
{
    private readonly WeeklySchedule _schedule = new WeeklySchedule();
    private static DateTime Now => DateTime.UtcNow;

    #region SINGLE DAY CASES

    [Fact]
    public void SingleDay_FutureTime_Today()
    {
        var current = Now;
        var execution = current.AddHours(1).TimeOfDay;
        var model = Build(current, current.DayOfWeek.ToString(), execution);

        var result = _schedule.GetNextExecutionUtc(model);

        Assert.Equal(current.Date + execution, result);
    }

    [Fact]
    public void SingleDay_EqualTime_NextWeek()
    {
        var current = Now;
        var execution = current.TimeOfDay; // equal
        var model = Build(current, current.DayOfWeek.ToString(), execution);

        var result = _schedule.GetNextExecutionUtc(model);

        Assert.Equal(current.Date.AddDays(7) + execution, result);
    }

    [Fact]
    public void SingleDay_PastTime_NextWeek()
    {
        var current = Now;
        var execution = current.AddHours(-1).TimeOfDay;
        var model = Build(current, current.DayOfWeek.ToString(), execution);

        var result = _schedule.GetNextExecutionUtc(model);

        Assert.Equal(current.Date.AddDays(7) + execution, result);
    }

    #endregion

    #region MULTIPLE DAY CASES

    [Fact]
    public void MultipleDays_FutureTime_Today()
    {
        var current = Now;
        var execution = current.AddHours(1).TimeOfDay;
        var today = current.DayOfWeek;
        var next = (DayOfWeek)(((int)today + 2) % 7);
        // Unsorted input intentionally
        var model = Build(current,$"{next},{today}",execution);

        var result = _schedule.GetNextExecutionUtc(model);

        Assert.Equal(current.Date.Add(execution),result);
    }

    [Fact]
    public void MultipleDays_EqualTime_ReturnsNextDay()
    {
        var current = Now;
        var execution = current.TimeOfDay;
        var next = NextDay(current.DayOfWeek, 1);
        var model = Build(current,$"{current.DayOfWeek},{next}",execution);

        var result = _schedule.GetNextExecutionUtc(model);

        Assert.Equal(NextDate(current, next) + execution, result);
    }

    [Fact]
    public void MultipleDays_PastTime_ReturnsNextDay()
    {
        var current = Now;
        var execution = current.AddHours(-1).TimeOfDay;
        var next = NextDay(current.DayOfWeek, 1);
        var model = Build(current,$"{current.DayOfWeek},{next}",execution);

        var result = _schedule.GetNextExecutionUtc(model);

        Assert.Equal(NextDate(current, next) + execution, result);
    }

    #endregion

    #region CROSSED LAST DAY CASES

    [Fact]
    public void MultipleDays_EqualTime_CrossedLastDay_NextWeekFirstDay()
    {
        var current = Now;
        var execution = current.TimeOfDay;
        var prev = NextDay(current.DayOfWeek, 6); // previous day
        // both are already passed or equal
        var model = Build(current,$"{prev},{current.DayOfWeek}",execution);

        var result = _schedule.GetNextExecutionUtc(model);

        Assert.Equal(NextDate(current, prev) + execution, result);
    }

    [Fact]
    public void MultipleDays_PastTime_CrossedLastDay_NextWeekFirstDay()
    {
        var current = Now;
        var execution = current.AddHours(-2).TimeOfDay;
        var prev = NextDay(current.DayOfWeek, 6);
        var model = Build(current,$"{prev},{current.DayOfWeek}",execution);

        var result = _schedule.GetNextExecutionUtc(model);

        Assert.Equal(NextDate(current, prev) + execution, result);
    }

    #endregion

    #region Helpers

    private static UnProcessedApointmentSchedule Build(
    DateTime current,
    string days,
    TimeSpan time)
    => new()
    {
        CurrentUtc = current,
        SelectedDays = days,
        UtcExecutionTime = time
    };

    private static DayOfWeek NextDay(DayOfWeek day, int offset)
        => (DayOfWeek)(((int)day + offset) % 7);

    private static DateTime NextDate(DateTime start, DayOfWeek day)
    {
        int diff = ((int)day - (int)start.DayOfWeek + 7) % 7;

        return start.Date.AddDays(diff);
    }

    #endregion
}