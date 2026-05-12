namespace ClearingHouse.SharedKernel.Domain.Enums;

/// <summary>
/// Represents the status of a file processing batch.
/// </summary>
public enum BatchStatus
{
    /// <summary>Batch has been created but not yet started.</summary>
    Created = 0,

    /// <summary>Batch processing is in progress.</summary>
    InProgress = 1,

    /// <summary>All files in the batch have been successfully processed.</summary>
    Completed = 2,

    /// <summary>Some files in the batch were processed successfully, others failed.</summary>
    PartiallyCompleted = 3,

    /// <summary>Batch processing has failed.</summary>
    Failed = 4
}
