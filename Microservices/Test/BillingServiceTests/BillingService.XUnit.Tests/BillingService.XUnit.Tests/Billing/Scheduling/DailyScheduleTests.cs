using BillingService.Domain.Scheduling.Models;
using BillingService.Domain.Scheduling.Strategies;
using System;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Scheduling;

public class DailyScheduleTests
{
    private readonly DailySchedule _sut;

    public DailyScheduleTests()
    {
        _sut = new DailySchedule();
    }

    [Theory]
    [InlineData(10, 0)]   // Execution time in future -> today
    [InlineData(-10, 1)]  // Execution time passed -> next day
    [InlineData(0, 1)]    // Equal -> next day (as per current logic)
    public void GetNextExecutionUtc_ShouldReturnExpectedExecution(int executionOffsetMinutes,int expectedDayOffset)
    {
        var now = DateTime.UtcNow;
        var model = BuildModel(now, executionOffsetMinutes);
        var expected = CalculateExpected(now, model.UtcExecutionTime, expectedDayOffset);

        var result = _sut.GetNextExecutionUtc(model);

        Assert.Equal(expected, result);
    }

    #region Helpers

    private static UnProcessedApointmentSchedule BuildModel( DateTime now,int executionOffsetMinutes)
    {
        return new UnProcessedApointmentSchedule
        {
            CurrentUtc = now,
            UtcExecutionTime = now.AddMinutes(executionOffsetMinutes).TimeOfDay,
        };
    }

    private static DateTime CalculateExpected( DateTime now,TimeSpan executionTime,int dayOffset)
    {
        return now.Date.AddDays(dayOffset) + executionTime;
    }

    #endregion
}