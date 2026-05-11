namespace ClearingHouse.SharedKernel.Enums;

public enum FileProcessingStatus
{
    Pending = 0,
    Downloading = 1,
    Downloaded = 2,
    Uploading = 3,
    Uploaded = 4,
    Processing = 5,
    Processed = 6,
    Failed = 7,
    PartiallyProcessed = 8,
    Archived = 9,
    DeadLettered = 10,
    RetryPending = 11,
    Cancelled = 12
}
