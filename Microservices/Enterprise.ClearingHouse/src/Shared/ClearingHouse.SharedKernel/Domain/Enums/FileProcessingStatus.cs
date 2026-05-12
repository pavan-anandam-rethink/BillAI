namespace ClearingHouse.SharedKernel.Domain.Enums;

/// <summary>
/// Represents the processing status of an EDI file throughout its lifecycle.
/// </summary>
public enum FileProcessingStatus
{
    /// <summary>File is pending pickup or processing.</summary>
    Pending = 0,

    /// <summary>File has been ingested from the clearinghouse.</summary>
    Ingested = 1,

    /// <summary>File has been queued for processing.</summary>
    Queued = 2,

    /// <summary>File is currently being processed.</summary>
    Processing = 3,

    /// <summary>File has been successfully processed.</summary>
    Processed = 4,

    /// <summary>File processing has failed.</summary>
    Failed = 5,

    /// <summary>File has been archived after successful processing.</summary>
    Archived = 6,

    /// <summary>File has been moved to dead letter after exhausting retries.</summary>
    DeadLettered = 7
}
