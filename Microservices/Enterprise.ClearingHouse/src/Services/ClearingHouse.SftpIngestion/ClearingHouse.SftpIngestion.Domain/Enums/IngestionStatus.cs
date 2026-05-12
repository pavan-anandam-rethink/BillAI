namespace ClearingHouse.SftpIngestion.Domain.Enums;

/// <summary>
/// Represents the current status of an SFTP ingestion job.
/// </summary>
public enum IngestionStatus
{
    /// <summary>The job is idle and waiting for its next scheduled execution.</summary>
    Idle = 0,

    /// <summary>The job is currently polling the SFTP endpoint for new files.</summary>
    Polling = 1,

    /// <summary>The job is downloading discovered files from the SFTP endpoint.</summary>
    Downloading = 2,

    /// <summary>The job is uploading files to Azure Blob Storage.</summary>
    Uploading = 3,

    /// <summary>The job is publishing events to the Service Bus.</summary>
    Publishing = 4,

    /// <summary>The job has completed successfully.</summary>
    Completed = 5,

    /// <summary>The job has failed due to an error.</summary>
    Failed = 6,

    /// <summary>The job has timed out.</summary>
    TimedOut = 7
}
