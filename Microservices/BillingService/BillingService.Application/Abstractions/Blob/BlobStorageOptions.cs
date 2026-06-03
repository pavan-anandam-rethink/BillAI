namespace BillingService.Application.Abstractions.Blob;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    /// <summary>Azure Blob Storage connection string or managed-identity endpoint.</summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>Default container used when no container is specified explicitly.</summary>
    public string DefaultContainerName { get; init; } = "billing-artifacts";

    /// <summary>Retry count for transient blob operations.</summary>
    public int MaxRetryCount { get; init; } = 3;

    /// <summary>Delay (seconds) between retry attempts (exponential base).</summary>
    public double RetryDelaySeconds { get; init; } = 1.0;
}
