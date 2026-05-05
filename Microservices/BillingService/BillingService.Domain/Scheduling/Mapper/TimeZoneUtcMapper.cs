namespace BillingService.Domain.Scheduling.Mapper;

using System;

public static class TimeZoneUtcMapper
{
    public static TimeSpan ConvertToUtcTime(this string time, string timeZoneId)
    {
        if (!TimeSpan.TryParse(time, out TimeSpan localTime))
            throw new ArgumentException("Invalid time format.");

        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId) 
                       ?? throw new TimeZoneNotFoundException($"TimeZone not found: {timeZoneId}");
       
        DateTime referenceDate = DateTime.UtcNow.Date + localTime;
        DateTime localDateTime = DateTime.SpecifyKind(referenceDate, DateTimeKind.Unspecified);

        DateTime utc = TimeZoneInfo.ConvertTimeToUtc(localDateTime, tz);

        return utc.TimeOfDay;
    }

    //truncate the value to 3-digit milliseconds to date in DATETIME2(3) format
    public static DateTime TrimToMilliseconds(this DateTime dt)
    {
        var trimDate = new DateTime(dt.Ticks - (dt.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        return trimDate;
    }

    public static string ToHourMinute(this string time)
    {
        if (string.IsNullOrWhiteSpace(time))
            return time;

        return TimeSpan.TryParse(time, out var ts)
            ? ts.ToString(@"hh\:mm")
            : throw new ArgumentException("Invalid time format");
    }
}