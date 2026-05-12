namespace ClearingHouse.SharedKernel.Infrastructure.ServiceBus;

/// <summary>
/// Constants for Azure Service Bus topic, subscription, and queue names used across microservices.
/// </summary>
public static class ServiceBusConstants
{
    /// <summary>
    /// Topic names for publishing domain events.
    /// </summary>
    public static class Topics
    {
        /// <summary>Topic for file ingestion events.</summary>
        public const string FileIngested = "file-ingested";

        /// <summary>Topic for file processed events.</summary>
        public const string FileProcessed = "file-processed";

        /// <summary>Topic for file processing failure events.</summary>
        public const string FileProcessingFailed = "file-processing-failed";

        /// <summary>Topic for batch created events.</summary>
        public const string BatchCreated = "batch-created";

        /// <summary>Topic for batch completed events.</summary>
        public const string BatchCompleted = "batch-completed";

        /// <summary>Topic for clearinghouse health events.</summary>
        public const string ClearinghouseHealth = "clearinghouse-health";
    }

    /// <summary>
    /// Subscription names for consuming events.
    /// </summary>
    public static class Subscriptions
    {
        /// <summary>Subscription for the processing engine service.</summary>
        public const string ProcessingEngine = "processing-engine";

        /// <summary>Subscription for the monitoring service.</summary>
        public const string Monitoring = "monitoring";

        /// <summary>Subscription for the notification service.</summary>
        public const string Notifications = "notifications";

        /// <summary>Subscription for the archival service.</summary>
        public const string Archival = "archival";

        /// <summary>Subscription for the analytics service.</summary>
        public const string Analytics = "analytics";
    }

    /// <summary>
    /// Queue names for point-to-point messaging.
    /// </summary>
    public static class Queues
    {
        /// <summary>Queue for file processing commands.</summary>
        public const string FileProcessingCommands = "file-processing-commands";

        /// <summary>Queue for batch processing commands.</summary>
        public const string BatchProcessingCommands = "batch-processing-commands";

        /// <summary>Queue for clearinghouse polling commands.</summary>
        public const string ClearinghousePolling = "clearinghouse-polling";

        /// <summary>Dead letter queue for failed messages.</summary>
        public const string DeadLetter = "dead-letter";

        /// <summary>Queue for retry processing.</summary>
        public const string RetryProcessing = "retry-processing";
    }
}
