namespace ClearingHouse.SftpIngestion.Infrastructure.Configuration;

/// <summary>
/// Configuration options for the SFTP Ingestion service.
/// </summary>
public sealed class SftpIngestionOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "SftpIngestion";

    /// <summary>
    /// Gets or sets the maximum number of concurrent polling operations.
    /// </summary>
    public int MaxConcurrentPolls { get; set; } = 3;

    /// <summary>
    /// Gets or sets the default polling interval in seconds.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the SFTP connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the SFTP operation timeout in seconds.
    /// </summary>
    public int OperationTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for transient failures.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the maximum number of SFTP connections per host.
    /// </summary>
    public int MaxConnectionsPerHost { get; set; } = 5;

    /// <summary>
    /// Gets or sets the Azure Blob Storage connection string.
    /// </summary>
    public string BlobStorageConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure Service Bus connection string.
    /// </summary>
    public string ServiceBusConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Service Bus topic name for ingestion events.
    /// </summary>
    public string ServiceBusTopicName { get; set; } = "edi-file-ingested";

    /// <summary>
    /// Gets or sets the SQL Server connection string.
    /// </summary>
    public string DatabaseConnectionString { get; set; } = string.Empty;
}
