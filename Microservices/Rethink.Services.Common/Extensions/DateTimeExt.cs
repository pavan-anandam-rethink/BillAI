using Quartz;
using System;
using System.Globalization;
using TimeZoneConverter;

namespace Rethink.Services.Common.Extensions
{
    public static class DateTimeExt
    {
        public const string EasternTimezoneName = "Eastern Standard Time";
        public static DateTimeOffset EvenIncrementDateAfterNow(int increment)
        {
            if (increment == 60)
                return DateBuilder.EvenHourDateAfterNow();

            var now = DateBuilder.EvenMinuteDateAfterNow();

            for (var i = 0; i < 60; i++)
            {
                var d = now.AddMinutes(-1 * now.Minute).AddMinutes(i * (increment % 60));

                if (d > now)
                    return d;
            }

            return now;
        }
        public static DateTime GetEasternDateTime(DateTime? dateTimeEntry = null)
        {
            var easternZone = TZConvert.GetTimeZoneInfo(EasternTimezoneName);
            var easternTime = TimeZoneInfo.ConvertTimeFromUtc(dateTimeEntry ?? DateTime.UtcNow, easternZone);

            return easternTime;
        }
        public static int GetEasternDateTimeUtcOffsetHours(DateTime? dateTimeEntry = null)
        {
            var easternZone = TZConvert.GetTimeZoneInfo(EasternTimezoneName);
            var timeSpan = easternZone.GetUtcOffset(dateTimeEntry ?? DateTime.UtcNow);

            return timeSpan.Hours;
        }

        public static TimeZoneInfo GetSystemTimeZoneById(string windowsOrIanaTimeZoneId)
        {
            return TZConvert.GetTimeZoneInfo(windowsOrIanaTimeZoneId);
        }

        public static string ToEdiString(this DateTime dateTime)
        {
            return $"{dateTime:yyyyMMdd}";
        }

        public static DateTime? FromEdiString(string dateTimeStr)
        {
            DateTime.TryParseExact(dateTimeStr, "yyyyMMdd", null, DateTimeStyles.None, out var result);
            return result;
        }

        // Returns empty string if input is null or whitespace
        public static string TrimAndEmptyIfNullOrWhitespace(this string input)
        {
            return string.IsNullOrWhiteSpace(input) ? string.Empty : input.Trim();
        }
        // Returns null if input is null or whitespace
        public static string TrimAndNullIfWhitespace(this string input)
        {
            return string.IsNullOrWhiteSpace(input) ? null : input.Trim();
        }

    }
}