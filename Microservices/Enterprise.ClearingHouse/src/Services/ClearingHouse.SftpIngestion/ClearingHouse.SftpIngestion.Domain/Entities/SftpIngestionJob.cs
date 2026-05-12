using ClearingHouse.SharedKernel.Domain.Entities;
using ClearingHouse.SharedKernel.Domain.ValueObjects;
using ClearingHouse.SftpIngestion.Domain.Enums;
using ClearingHouse.SftpIngestion.Domain.Events;

namespace ClearingHouse.SftpIngestion.Domain.Entities;

/// <summary>
/// Aggregate root representing an SFTP ingestion job that polls clearinghouse endpoints
/// and downloads EDI files for processing.
/// </summary>
public sealed class SftpIngestionJob : AggregateRoot
{
    /// <summary>
    /// Gets the clearinghouse identifier this job is associated with.
    /// </summary>
    public ClearinghouseIdentifier ClearinghouseIdentifier { get; private set; } = null!;

    /// <summary>
    /// Gets the polling schedule as a cron expression.
    /// </summary>
    public string PollingSchedule { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the UTC timestamp when the endpoint was last polled.
    /// </summary>
    public DateTime? LastPolledAt { get; private set; }

    /// <summary>
    /// Gets the current status of the ingestion job.
    /// </summary>
    public IngestionStatus Status { get; private set; } = IngestionStatus.Idle;

    /// <summary>
    /// Gets the number of files discovered during the last poll.
    /// </summary>
    public int FilesDiscovered { get; private set; }

    /// <summary>
    /// Gets the number of files successfully processed during the last poll.
    /// </summary>
    public int FilesProcessed { get; private set; }

    /// <summary>
    /// Gets the error message if the job failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the correlation ID for distributed tracing.
    /// </summary>
    public CorrelationId CorrelationId { get; private set; } = null!;

    private SftpIngestionJob() { }

    /// <summary>
    /// Creates a new SFTP ingestion job for the specified clearinghouse.
    /// </summary>
    /// <param name="clearinghouseIdentifier">The clearinghouse to poll.</param>
    /// <param name="pollingSchedule">The cron expression for the polling schedule.</param>
    /// <param name="correlationId">The correlation ID for tracing.</param>
    /// <returns>A new <see cref="SftpIngestionJob"/> instance.</returns>
    public static SftpIngestionJob Create(
        ClearinghouseIdentifier clearinghouseIdentifier,
        string pollingSchedule,
        CorrelationId correlationId)
    {
        ArgumentNullException.ThrowIfNull(clearinghouseIdentifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(pollingSchedule);
        ArgumentNullException.ThrowIfNull(correlationId);

        var job = new SftpIngestionJob
        {
            ClearinghouseIdentifier = clearinghouseIdentifier,
            PollingSchedule = pollingSchedule,
            CorrelationId = correlationId,
            Status = IngestionStatus.Idle
        };

        job.AddDomainEvent(new IngestionJobCreatedEvent(job.Id, clearinghouseIdentifier, correlationId));
        return job;
    }

    /// <summary>
    /// Transitions the job to the Polling status.
    /// </summary>
    public void StartPolling()
    {
        Status = IngestionStatus.Polling;
        LastPolledAt = DateTime.UtcNow;
        IncrementVersion();
    }

    /// <summary>
    /// Records the number of files discovered during polling.
    /// </summary>
    /// <param name="count">The number of files discovered.</param>
    public void RecordFilesDiscovered(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        FilesDiscovered = count;
        Status = count > 0 ? IngestionStatus.Downloading : IngestionStatus.Completed;
        IncrementVersion();
    }

    /// <summary>
    /// Transitions the job to the Uploading status.
    /// </summary>
    public void StartUploading()
    {
        Status = IngestionStatus.Uploading;
        IncrementVersion();
    }

    /// <summary>
    /// Transitions the job to the Publishing status.
    /// </summary>
    public void StartPublishing()
    {
        Status = IngestionStatus.Publishing;
        IncrementVersion();
    }

    /// <summary>
    /// Records a successfully processed file.
    /// </summary>
    public void RecordFileProcessed()
    {
        FilesProcessed++;
        IncrementVersion();
    }

    /// <summary>
    /// Marks the job as completed.
    /// </summary>
    public void Complete()
    {
        Status = IngestionStatus.Completed;
        ErrorMessage = null;
        IncrementVersion();
        AddDomainEvent(new IngestionJobCompletedEvent(Id, ClearinghouseIdentifier, FilesProcessed, CorrelationId));
    }

    /// <summary>
    /// Marks the job as failed with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error details.</param>
    public void Fail(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        Status = IngestionStatus.Failed;
        ErrorMessage = errorMessage;
        IncrementVersion();
        AddDomainEvent(new IngestionJobFailedEvent(Id, ClearinghouseIdentifier, errorMessage, CorrelationId));
    }

    /// <summary>
    /// Marks the job as timed out.
    /// </summary>
    public void TimedOut()
    {
        Status = IngestionStatus.TimedOut;
        ErrorMessage = "Ingestion job timed out.";
        IncrementVersion();
    }
}
