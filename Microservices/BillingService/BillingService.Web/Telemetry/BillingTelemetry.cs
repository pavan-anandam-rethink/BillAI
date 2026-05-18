using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BillingService.Web.Telemetry
{
    public static class BillingTelemetry
    {
        public const string ActivitySourceName = "BillingService";
        public const string MeterName = "BillingService";

        public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
        public static readonly Meter Meter = new(MeterName);

        public static readonly Histogram<double> HttpServerDurationMs =
            Meter.CreateHistogram<double>(
                "billing.http.server.duration",
                "ms",
                "Billing API request duration measured at the compatibility host boundary.");

        public static readonly Counter<long> HttpServerRequests =
            Meter.CreateCounter<long>(
                "billing.http.server.requests",
                description: "Billing API request count measured at the compatibility host boundary.");
    }
}
