using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ClearingHouse.SharedKernel.Observability;

public static class DiagnosticsConfig
{
    public static readonly ActivitySource ActivitySource = new(TelemetryConstants.ServiceName, "1.0.0");
    public static readonly Meter Meter = new(TelemetryConstants.ServiceName, "1.0.0");

    // Counters
    public static readonly Counter<long> FilesIngestedCounter = Meter.CreateCounter<long>(TelemetryConstants.Metrics.FilesIngested);
    public static readonly Counter<long> FilesProcessedCounter = Meter.CreateCounter<long>(TelemetryConstants.Metrics.FilesProcessed);
    public static readonly Counter<long> FilesFailedCounter = Meter.CreateCounter<long>(TelemetryConstants.Metrics.FilesFailed);
    public static readonly Counter<long> SftpErrorsCounter = Meter.CreateCounter<long>(TelemetryConstants.Metrics.SftpConnectionErrors);

    // Histograms
    public static readonly Histogram<double> ProcessingDurationHistogram = Meter.CreateHistogram<double>(
        TelemetryConstants.Metrics.ProcessingDuration, "ms", "Duration of file processing in milliseconds");

    // Gauges
    public static readonly ObservableGauge<int> QueueDepthGauge = Meter.CreateObservableGauge<int>(
        TelemetryConstants.Metrics.QueueDepth, () => 0);
}
