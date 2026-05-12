using ClearingHouse.EdiProcessing.Domain.Enums;
using ClearingHouse.EdiProcessing.Domain.Events;
using ClearingHouse.EdiProcessing.Domain.ValueObjects;
using ClearingHouse.SharedKernel.Domain.Entities;
using ClearingHouse.SharedKernel.Domain.Enums;
using ClearingHouse.SharedKernel.Domain.ValueObjects;

namespace ClearingHouse.EdiProcessing.Domain.Entities;

/// <summary>
/// Aggregate root representing an EDI file being processed through the clearing house pipeline.
/// </summary>
public class EdiFile : AggregateRoot
{
    private readonly List<EdiSegment> _segments = new();
    private readonly List<EdiProcessingError> _errors = new();
    private readonly List<ClaimTransaction> _claimTransactions = new();

    /// <summary>Gets the correlation identifier for distributed tracing.</summary>
    public CorrelationId CorrelationId { get; private set; } = null!;

    /// <summary>Gets the name of the EDI file.</summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>Gets the file reference for blob storage, if set.</summary>
    public FileReference? FileReference { get; private set; }

    /// <summary>Gets the EDI transaction type (e.g., 837, 835).</summary>
    public EdiTransactionType EdiTransactionType { get; private set; } = null!;

    /// <summary>Gets the clearinghouse type.</summary>
    public ClearinghouseType ClearinghouseType { get; private set; }

    /// <summary>Gets the current processing status.</summary>
    public EdiProcessingStatus Status { get; private set; }

    /// <summary>Gets the total number of segments in the file.</summary>
    public int TotalSegments { get; private set; }

    /// <summary>Gets the number of segments that have been processed.</summary>
    public int ProcessedSegments { get; private set; }

    /// <summary>Gets the count of errors encountered during processing.</summary>
    public int ErrorCount { get; private set; }

    /// <summary>Gets the date and time when processing started.</summary>
    public DateTime? ProcessingStartedAt { get; private set; }

    /// <summary>Gets the date and time when processing completed.</summary>
    public DateTime? ProcessingCompletedAt { get; private set; }

    /// <summary>Gets the read-only collection of parsed EDI segments.</summary>
    public IReadOnlyCollection<EdiSegment> Segments => _segments.AsReadOnly();

    /// <summary>Gets the read-only collection of processing errors.</summary>
    public IReadOnlyCollection<EdiProcessingError> Errors => _errors.AsReadOnly();

    /// <summary>Gets the read-only collection of extracted claim transactions.</summary>
    public IReadOnlyCollection<ClaimTransaction> ClaimTransactions => _claimTransactions.AsReadOnly();

    /// <summary>
    /// Creates a new <see cref="EdiFile"/> aggregate root.
    /// </summary>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="ediTransactionType">The EDI transaction type.</param>
    /// <param name="clearinghouseType">The clearinghouse type.</param>
    /// <returns>A new <see cref="EdiFile"/> instance.</returns>
    public static EdiFile Create(
        CorrelationId correlationId,
        string fileName,
        EdiTransactionType ediTransactionType,
        ClearinghouseType clearinghouseType)
    {
        ArgumentNullException.ThrowIfNull(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(ediTransactionType);

        var ediFile = new EdiFile
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            FileName = fileName,
            EdiTransactionType = ediTransactionType,
            ClearinghouseType = clearinghouseType,
            Status = EdiProcessingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        ediFile.AddDomainEvent(new EdiFileCreatedEvent(
            ediFile.Id,
            fileName,
            ediTransactionType,
            correlationId,
            DateTime.UtcNow));

        return ediFile;
    }

    /// <summary>
    /// Starts processing the EDI file.
    /// </summary>
    public void StartProcessing()
    {
        Status = EdiProcessingStatus.Parsing;
        ProcessingStartedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        AddDomainEvent(new EdiFileProcessingStartedEvent(
            Id,
            CorrelationId,
            DateTime.UtcNow));
    }

    /// <summary>
    /// Transitions the file status to Parsing.
    /// </summary>
    public void MarkParsing()
    {
        Status = EdiProcessingStatus.Parsing;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    /// <summary>
    /// Transitions the file status to Validating.
    /// </summary>
    public void MarkValidating()
    {
        Status = EdiProcessingStatus.Validating;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    /// <summary>
    /// Transitions the file status to Transforming.
    /// </summary>
    public void MarkTransforming()
    {
        Status = EdiProcessingStatus.Transforming;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    /// <summary>
    /// Marks the file as successfully completed with the given processing metrics.
    /// </summary>
    /// <param name="metrics">The processing metrics.</param>
    public void MarkCompleted(ProcessingMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        Status = EdiProcessingStatus.Completed;
        TotalSegments = metrics.TotalSegments;
        ProcessedSegments = metrics.ProcessedSegments;
        ProcessingCompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        AddDomainEvent(new EdiFileCompletedEvent(
            Id,
            metrics,
            CorrelationId,
            DateTime.UtcNow));
    }

    /// <summary>
    /// Marks the file as partially completed with the given processing metrics.
    /// </summary>
    /// <param name="metrics">The processing metrics.</param>
    public void MarkPartiallyCompleted(ProcessingMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        Status = EdiProcessingStatus.PartiallyCompleted;
        TotalSegments = metrics.TotalSegments;
        ProcessedSegments = metrics.ProcessedSegments;
        ErrorCount = metrics.FailedSegments;
        ProcessingCompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        AddDomainEvent(new EdiFileCompletedEvent(
            Id,
            metrics,
            CorrelationId,
            DateTime.UtcNow));
    }

    /// <summary>
    /// Marks the file processing as failed with the given error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    public void MarkFailed(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        Status = EdiProcessingStatus.Failed;
        ProcessingCompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        AddDomainEvent(new EdiFileFailedEvent(
            Id,
            errorMessage,
            CorrelationId,
            DateTime.UtcNow));
    }

    /// <summary>
    /// Increments the count of successfully processed segments.
    /// </summary>
    public void IncrementProcessedSegments()
    {
        ProcessedSegments++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments the error count.
    /// </summary>
    public void IncrementErrorCount()
    {
        ErrorCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the file reference for blob storage.
    /// </summary>
    /// <param name="fileReference">The file reference.</param>
    public void SetFileReference(FileReference fileReference)
    {
        ArgumentNullException.ThrowIfNull(fileReference);
        FileReference = fileReference;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    /// <summary>
    /// Updates the processing metrics for the file.
    /// </summary>
    /// <param name="totalSegments">The total number of segments.</param>
    /// <param name="processedSegments">The number of processed segments.</param>
    /// <param name="errors">The number of errors.</param>
    public void UpdateMetrics(int totalSegments, int processedSegments, int errors)
    {
        TotalSegments = totalSegments;
        ProcessedSegments = processedSegments;
        ErrorCount = errors;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// </summary>
    private EdiFile() { }
}
