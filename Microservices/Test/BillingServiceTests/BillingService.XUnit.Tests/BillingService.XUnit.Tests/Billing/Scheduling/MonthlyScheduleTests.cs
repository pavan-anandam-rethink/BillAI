using BillingService.Domain.Scheduling.Models;
using BillingService.Domain.Scheduling.Strategies;
using System;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Scheduling;

public class MonthlyScheduleTests
{
    private readonly MonthlySchedule _schedule = new MonthlySchedule();

    private readonly DateTime _firstDay;
    private readonly DateTime _nextMonthFirstDay;
    private readonly DateTime _lastDay;
    private readonly DateTime _nextMonthLastDay;
    private readonly DateTime _firstDayNextMonth;

    public MonthlyScheduleTests()
    {
        var now = DateTime.UtcNow;

        _firstDay = new DateTime(now.Year, now.Month, 1, 10, 0, 0, DateTimeKind.Utc);
        _nextMonthFirstDay = new DateTime(now.Year, now.Month, 1, 10, 0, 0, DateTimeKind.Utc)
                        .AddMonths(1);

        _lastDay = new DateTime(
            now.Year,
            now.Month,
            DateTime.DaysInMonth(now.Year, now.Month),
            10, 0, 0,
            DateTimeKind.Utc);

        _nextMonthLastDay = new DateTime(now.Year, now.Month, 1, 10, 0, 0, DateTimeKind.Utc).AddMonths(2).Date.AddDays(-1);

        var next = now.AddMonths(1);

        _firstDayNextMonth = new DateTime(
            next.Year,
            next.Month,
            1,
            10, 0, 0,
            DateTimeKind.Utc);
    }

    #region FIRST DAY

    [Fact]
    public void When_FirstDay_TimePassed_Schedule_NextMonthFirsttDay()
    {
        var model = CreateSchedule(_firstDay, 9);
        var expected = ReplaceHour(_nextMonthFirstDay, 9);
        model.Frequency = 1;

        var result = _schedule.GetNextExecutionUtc(model);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void When_FirstDay_TimeEqual_Schedule_NextMonthFirsttDay()
    {
        var model = CreateSchedule(_firstDay, 10);
        var expected = ReplaceHour(_nextMonthFirstDay, 10);
        model.Frequency = 1;

        var result = _schedule.GetNextExecutionUtc(model);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void When_FirstDay_TimeFuture_Schedule_Today()
    {
        var model = CreateSchedule(_firstDay, 12);
        var expected = ReplaceHour(_firstDay, 12);
        model.Frequency = 1;

        var result = _schedule.GetNextExecutionUtc(model);

        Assert.Equal(expected, result);
    }

    #endregion

    #region LAST DAY

    [Fact]
    public void When_LastDay_TimePassed_Schedule_NextMonthLasttDay()
    {
        var model = CreateSchedule(_lastDay, 9);
        var expected = ReplaceHour(_nextMonthLastDay, 9);
        model.Frequency = 2;

        var result = _schedule.GetNextExecutionUtc(model);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void When_LastDay_TimeEqual_Schedule_FirstDayNextMonth()
    {
        var model = CreateSchedule(_lastDay, 10);
        var expected = ReplaceHour(_nextMonthLastDay, 10);
        model.Frequency = 2;

        var result = _schedule.GetNextExecutionUtc(model);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void When_LastDay_TimeFuture_Schedule_Today()
    {
        var model = CreateSchedule(_lastDay, 12);
        var expected = ReplaceHour(_lastDay, 12);
        model.Frequency = 2;

        var result = _schedule.GetNextExecutionUtc(model);

        Assert.Equal(expected, result);
    }

    #endregion

    #region Any Middle Day

    [Fact]
    public void When_MiddleDay_Schedule_LastDayOfMonth()
    {
        var now = DateTime.UtcNow;
        var middleDay = new DateTime(now.Year,now.Month,15,10, 0, 0,DateTimeKind.Utc);
        var model = CreateSchedule(middleDay, 9);
        model.Frequency = 2;
        var expected = ReplaceHour(_lastDay, 9);

        var result = _schedule.GetNextExecutionUtc(model);

        Assert.Equal(expected, result);
    }

    #endregion

    #region Helpers

    private static UnProcessedApointmentSchedule CreateSchedule(DateTime currentUtc, int executionHour)
    {
        return new UnProcessedApointmentSchedule
        {
            CurrentUtc = currentUtc,
            UtcExecutionTime = new TimeSpan(executionHour, 0, 0)
        };
    }

    private static DateTime ReplaceHour(DateTime date, int hour)
    {
        return new DateTime(date.Year,date.Month,date.Day,hour, 0, 0,DateTimeKind.Utc);
    }

    #endregion
}