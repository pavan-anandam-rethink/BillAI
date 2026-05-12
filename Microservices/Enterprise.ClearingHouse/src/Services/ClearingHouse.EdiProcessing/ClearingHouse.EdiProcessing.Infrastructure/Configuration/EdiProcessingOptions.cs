namespace ClearingHouse.EdiProcessing.Infrastructure.Configuration;

/// <summary>
/// Configuration options for the EDI Processing service.
/// </summary>
public sealed class EdiProcessingOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "EdiProcessing";

    /// <summary>Gets or sets the database connection string.</summary>
    public string DatabaseConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets the Azure Blob Storage connection string.</summary>
    public string BlobStorageConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets the Azure Service Bus connection string.</summary>
    public string ServiceBusConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets the Service Bus topic name for file-ingested events.</summary>
    public string TopicName { get; set; } = "file-ingested";

    /// <summary>Gets or sets the Service Bus subscription name.</summary>
    public string SubscriptionName { get; set; } = "edi-processing";

    /// <summary>Gets or sets the maximum retry count before dead-lettering.</summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>Gets or sets the channel capacity for concurrent processing.</summary>
    public int ChannelCapacity { get; set; } = 100;

    /// <summary>Gets or sets the buffer size for stream reading operations.</summary>
    public int BufferSize { get; set; } = 8192;
}
