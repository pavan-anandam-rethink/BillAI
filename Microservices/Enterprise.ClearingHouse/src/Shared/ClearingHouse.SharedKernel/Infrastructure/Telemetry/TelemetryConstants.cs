namespace ClearingHouse.SharedKernel.Infrastructure.Telemetry;

/// <summary>
/// Constants for OpenTelemetry instrumentation across the clearinghouse platform.
/// </summary>
public static class TelemetryConstants
{
    /// <summary>
    /// Activity source names for distributed tracing.
    /// </summary>
    public static class ActivitySources
    {
        /// <summary>Activity source for file ingestion operations.</summary>
        public const string FileIngestion = "ClearingHouse.FileIngestion";

        /// <summary>Activity source for file processing operations.</summary>
        public const string FileProcessing = "ClearingHouse.FileProcessing";

        /// <summary>Activity source for clearinghouse connector operations.</summary>
        public const string ClearinghouseConnector = "ClearingHouse.Connector";

        /// <summary>Activity source for batch operations.</summary>
        public const string BatchProcessing = "ClearingHouse.BatchProcessing";

        /// <summary>Activity source for blob storage operations.</summary>
        public const string BlobStorage = "ClearingHouse.BlobStorage";

        /// <summary>Activity source for service bus operations.</summary>
        public const string ServiceBus = "ClearingHouse.ServiceBus";
    }

    /// <summary>
    /// Meter names for metrics collection.
    /// </summary>
    public static class Meters
    {
        /// <summary>Meter for file ingestion metrics.</summary>
        public const string FileIngestion = "ClearingHouse.Metrics.FileIngestion";

        /// <summary>Meter for file processing metrics.</summary>
        public const string FileProcessing = "ClearingHouse.Metrics.FileProcessing";

        /// <summary>Meter for clearinghouse connector metrics.</summary>
        public const string Connector = "ClearingHouse.Metrics.Connector";

        /// <summary>Meter for batch processing metrics.</summary>
        public const string BatchProcessing = "ClearingHouse.Metrics.BatchProcessing";
    }

    /// <summary>
    /// Span attribute keys for enriching trace data.
    /// </summary>
    public static class Attributes
    {
        /// <summary>The correlation ID attribute.</summary>
        public const string CorrelationId = "clearinghouse.correlation_id";

        /// <summary>The EDI transaction type attribute.</summary>
        public const string TransactionType = "clearinghouse.transaction_type";

        /// <summary>The clearinghouse name attribute.</summary>
        public const string ClearinghouseName = "clearinghouse.name";

        /// <summary>The clearinghouse code attribute.</summary>
        public const string ClearinghouseCode = "clearinghouse.code";

        /// <summary>The file name attribute.</summary>
        public const string FileName = "clearinghouse.file_name";

        /// <summary>The file size attribute.</summary>
        public const string FileSize = "clearinghouse.file_size";

        /// <summary>The batch ID attribute.</summary>
        public const string BatchId = "clearinghouse.batch_id";

        /// <summary>The processing status attribute.</summary>
        public const string ProcessingStatus = "clearinghouse.processing_status";

        /// <summary>The transaction count attribute.</summary>
        public const string TransactionCount = "clearinghouse.transaction_count";

        /// <summary>The error code attribute.</summary>
        public const string ErrorCode = "clearinghouse.error_code";
    }
}
