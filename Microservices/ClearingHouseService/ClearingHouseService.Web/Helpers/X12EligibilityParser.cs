using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.EligibilityRequest;
using static Rethink.Services.Common.Utils.X12SegmentReader;

namespace ClearingHouseService.Web.Helpers
{
    public class X12EligibilityParser : IX12Parser<Eligibility271ParsedResponse>
    {
        public Eligibility271ParsedResponse Parse(string x12)
        {
            var result = new Eligibility271ParsedResponse();

            bool ebDatesFound = false;

            var segments = x12.Split('~', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());

            foreach (var segment in segments)
            {
                var elements = segment.Split('*');
                var segmentId = elements[0];

                // ---------------- AAA ----------------
                if (segmentId == X12SegmentIds.AAA)
                {
                    var rejectIndicator = elements.Length > 1 ? elements[1] : null;
                    var freeText = elements.Length > 2 ? elements[2] : null;
                    var rejectCode = elements.Length > 3 ? elements[3] : null;
                    var rejectType = elements.Length > 4 ? elements[4] : null;

                    if (rejectIndicator == "N") // Not Authorized / Rejected
                    {
                        // Map rejectCode to human-readable reason
                        string reason = MapRejectCodeToMessage(rejectCode, freeText);

                        // Throw exception or return in result object.
                        throw new InvalidOperationException(reason);
                    }

                    continue;
                }

                // ---------------- EB ----------------
                if (segmentId == X12SegmentIds.EB)
                {
                    var eb01 = elements[1];
                    var eb03 = elements.Length > 3 ? elements[3] : null;

                    if (eb01 == "1" && eb03 == "30")
                    {
                        result.CoverageStatus = ResolveCoverageStatus(eb01);

                    }
                }

                // ---------------- DTP ----------------
                if (segmentId == X12SegmentIds.DTP)
                {
                    var qualifier = elements[1];
                    var format = elements[2];
                    var value = elements.Length > 3 ? elements[3] : null;

                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    // EB-level (highest priority)
                    if (qualifier == X12DateQualifiers.BenefitBegin)
                    {
                        result.EffectiveStartDate = ParseSingleDate(value);
                        ebDatesFound = true;
                    }
                    else if (qualifier == X12DateQualifiers.BenefitEnd)
                    {
                        result.EffectiveEndDate = ParseSingleDate(value);
                        ebDatesFound = true;
                    }

                    // Subscriber-level (only if EB missing)
                    else if (!ebDatesFound &&
                             qualifier == X12DateQualifiers.SubscriberBegin)
                    {
                        result.SubscriberStartDate ??= ParseSingleDate(value);
                    }
                    else if (!ebDatesFound &&
                             qualifier == X12DateQualifiers.SubscriberEnd)
                    {
                        result.SubscriberEndDate ??= ParseSingleDate(value);
                    }

                    // 🔹 Plan-level (291)
                    else if (qualifier == X12DateQualifiers.PlanDates && format == "RD8")
                    {
                        var (startDate, endDate) = ParseRange(value);
                        result.PlanStartDate = startDate;
                        result.PlanEndDate = endDate;
                    }

                    continue;
                }


            }
            return result;
        }

        private string MapRejectCodeToMessage(string rejectCode, string freeText)
        {
            if (EligibilityRejectDictionary.RejectCodeMessages.TryGetValue(rejectCode, out var message))
            {
                return message;
            }

            return !string.IsNullOrWhiteSpace(freeText)
                        ? freeText
                        : $"Payer rejected with code {rejectCode}";
        }
    }
}
