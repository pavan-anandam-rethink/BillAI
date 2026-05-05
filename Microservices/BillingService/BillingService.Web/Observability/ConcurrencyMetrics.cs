using System.Diagnostics.Metrics;
using System.Threading;

namespace BillingService.Web.Observability;

internal static class ConcurrencyMetrics
{
    private static readonly Meter Meter = new("BillingService.Web.Performance", "1.0.0");
    private static long _activeRequests;

    private static readonly Histogram<double> RequestDurationMs = Meter.CreateHistogram<double>(
        "billing.http.server.duration",
        unit: "ms",
        description: "API request duration by endpoint.");

    private static readonly Counter<long> RequestErrors = Meter.CreateCounter<long>(
        "billing.http.server.errors",
        description: "Count of failed API requests.");

    private static readonly UpDownCounter<long> ActiveRequestsCounter = Meter.CreateUpDownCounter<long>(
        "billing.http.server.active_requests",
        description: "Current active API requests.");

    private static readonly Histogram<double> ExternalDependencyDurationMs = Meter.CreateHistogram<double>(
        "billing.http.client.duration",
        unit: "ms",
        description: "Outbound dependency call duration.");

    private static readonly Counter<long> ExternalDependencyErrors = Meter.CreateCounter<long>(
        "billing.http.client.errors",
        description: "Count of failed outbound dependency calls.");

    private static readonly Histogram<double> DbCommandDurationMs = Meter.CreateHistogram<double>(
        "billing.db.command.duration",
        unit: "ms",
        description: "Database command duration.");

    static ConcurrencyMetrics()
    {
        Meter.CreateObservableGauge(
            "billing.http.server.active_requests_gauge",
            () => new Measurement<long>(Volatile.Read(ref _activeRequests)));

        Meter.CreateObservableGauge(
            "billing.threadpool.queue_length",
            () => new Measurement<long>(ThreadPool.PendingWorkItemCount));
    }

    public static void RequestStarted(string method, string endpoint)
    {
        Interlocked.Increment(ref _activeRequests);
        ActiveRequestsCounter.Add(1, BuildHttpTags(method, endpoint, statusCode: null));
    }

    public static void RequestCompleted(string method, string endpoint, int statusCode, double elapsedMs, bool hasException)
    {
        RequestDurationMs.Record(elapsedMs, BuildHttpTags(method, endpoint, statusCode));

        if (hasException || statusCode >= 500)
        {
            RequestErrors.Add(1, BuildHttpTags(method, endpoint, statusCode));
        }

        ActiveRequestsCounter.Add(-1, BuildHttpTags(method, endpoint, statusCode));
        Interlocked.Decrement(ref _activeRequests);
    }

    public static void RecordExternalDependency(string dependency, string method, int? statusCode, double elapsedMs, bool success)
    {
        ExternalDependencyDurationMs.Record(elapsedMs, BuildDependencyTags(dependency, method, statusCode));
        if (!success)
        {
            ExternalDependencyErrors.Add(1, BuildDependencyTags(dependency, method, statusCode));
        }
    }

    public static void RecordDbCommand(string operation, double elapsedMs)
    {
        DbCommandDurationMs.Record(elapsedMs, new TagList { { "db.operation", operation } });
    }

    private static TagList BuildHttpTags(string method, string endpoint, int? statusCode)
    {
        var tags = new TagList
        {
            { "http.method", method },
            { "http.route", endpoint }
        };

        if (statusCode.HasValue)
        {
            tags.Add("http.status_code", statusCode.Value);
        }

        return tags;
    }

    private static TagList BuildDependencyTags(string dependency, string method, int? statusCode)
    {
        var tags = new TagList
        {
            { "dependency.host", dependency },
            { "http.method", method }
        };

        if (statusCode.HasValue)
        {
            tags.Add("http.status_code", statusCode.Value);
        }

        return tags;
    }
}
