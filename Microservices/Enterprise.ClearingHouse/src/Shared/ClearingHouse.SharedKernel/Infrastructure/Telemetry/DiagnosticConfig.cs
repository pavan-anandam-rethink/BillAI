using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ClearingHouse.SharedKernel.Infrastructure.Telemetry;

/// <summary>
/// Central configuration for diagnostic instrumentation (ActivitySource and Meter instances).
/// </summary>
public static class DiagnosticConfig
{
    /// <summary>
    /// ActivitySource for file ingestion tracing.
    /// </summary>
    public static readonly ActivitySource FileIngestionSource =
        new(TelemetryConstants.ActivitySources.FileIngestion, "1.0.0");

    /// <summary>
    /// ActivitySource for file processing tracing.
    /// </summary>
    public static readonly ActivitySource FileProcessingSource =
        new(TelemetryConstants.ActivitySources.FileProcessing, "1.0.0");

    /// <summary>
    /// ActivitySource for clearinghouse connector tracing.
    /// </summary>
    public static readonly ActivitySource ConnectorSource =
        new(TelemetryConstants.ActivitySources.ClearinghouseConnector, "1.0.0");

    /// <summary>
    /// ActivitySource for batch processing tracing.
    /// </summary>
    public static readonly ActivitySource BatchProcessingSource =
        new(TelemetryConstants.ActivitySources.BatchProcessing, "1.0.0");

    /// <summary>
    /// ActivitySource for blob storage tracing.
    /// </summary>
    public static readonly ActivitySource BlobStorageSource =
        new(TelemetryConstants.ActivitySources.BlobStorage, "1.0.0");

    /// <summary>
    /// ActivitySource for service bus tracing.
    /// </summary>
    public static readonly ActivitySource ServiceBusSource =
        new(TelemetryConstants.ActivitySources.ServiceBus, "1.0.0");

    /// <summary>
    /// Meter for file ingestion metrics.
    /// </summary>
    public static readonly Meter FileIngestionMeter =
        new(TelemetryConstants.Meters.FileIngestion, "1.0.0");

    /// <summary>
    /// Meter for file processing metrics.
    /// </summary>
    public static readonly Meter FileProcessingMeter =
        new(TelemetryConstants.Meters.FileProcessing, "1.0.0");

    /// <summary>
    /// Meter for clearinghouse connector metrics.
    /// </summary>
    public static readonly Meter ConnectorMeter =
        new(TelemetryConstants.Meters.Connector, "1.0.0");

    /// <summary>
    /// Meter for batch processing metrics.
    /// </summary>
    public static readonly Meter BatchProcessingMeter =
        new(TelemetryConstants.Meters.BatchProcessing, "1.0.0");
}
