using System;
using System.Globalization;

namespace Rethink.Services.Common.Utils
{
    public static class X12SegmentReader
    {

        /* Eligibility and benefit information Determine coverage status from EB01
         EB01 = 1–4 → Active
         EB01 = 5, M, N, Y, Z → Active (Conditional)
         EB01 = 6, 7, 8, I, U, V, W, X → Inactive
         */
        public static string ResolveCoverageStatus(string eb01)
        {
            return eb01 switch
            {
                "1" or "2" or "3" or "4" => "Active",
                "5" or "M" or "N" or "Y" or "Z" => "Active-Conditional",
                "6" or "7" or "8" or "I" or "U" or "V" or "W" or "X" => "Inactive",
                _ => "Unknown"
            };
        }

        public static (DateTime? start, DateTime? end) ParseDtp(string format, string value)
        {
            if (format == "D8")
                return (ParseSingleDate(value), null);

            if (format == "RD8")
            {
                var parts = value.Split('-');
                return (ParseSingleDate(parts[0]), ParseSingleDate(parts[1]));
            }
            return (null, null);
        }

        public static DateTime? ParseSingleDate(string value)
        {
            return DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : null;
        }

        public static class X12SegmentIds
        {
            public const string EB = "EB";
            public const string DTP = "DTP";
            public const string AAA = "AAA";
        }

        public static class X12DateQualifiers
        {
            // Benefit-level
            public const string BenefitBegin = "348";
            public const string BenefitEnd = "349";

            // Subscriber-level
            public const string SubscriberBegin = "356";
            public const string SubscriberEnd = "357";

            // Plan / coordination
            public const string PlanDates = "291";
        }

        public static (DateTime? Start, DateTime? End) ParseRange(string rd8Value)
        {
            if (string.IsNullOrWhiteSpace(rd8Value))
                return (null, null);

            var parts = rd8Value.Split('-', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                return (null, null);

            return (
                ParseSingle(parts[0]),
                ParseSingle(parts[1])
            );
        }
        public static DateTime? ParseSingle(string value)
        {
            return DateTime.TryParseExact(
                value,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date)
                ? date
                : null;
        }
    }
}
