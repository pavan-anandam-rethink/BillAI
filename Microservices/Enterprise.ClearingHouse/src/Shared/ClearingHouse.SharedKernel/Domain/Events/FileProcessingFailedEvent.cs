using ClearingHouse.SharedKernel.Domain.ValueObjects;

namespace ClearingHouse.SharedKernel.Domain.Events;

/// <summary>
/// Domain event raised when file processing fails.
/// </summary>
public sealed record FileProcessingFailedEvent : IDomainEvent
{
    /// <summary>
    /// Gets the reference to the file that failed processing.
    /// </summary>
    public FileReference FileReference { get; }

    /// <summary>
    /// Gets the EDI transaction type of the failed file.
    /// </summary>
    public EdiTransactionType TransactionType { get; }

    /// <inheritdoc />
    public CorrelationId CorrelationId { get; }

    /// <inheritdoc />
    public DateTime OccurredOn { get; }

    /// <summary>
    /// Gets the error message describing the failure.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Gets the error code for categorization.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Gets the stack trace if available.
    /// </summary>
    public string? StackTrace { get; }

    /// <summary>
    /// Gets the number of retry attempts made before failure.
    /// </summary>
    public int RetryCount { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileProcessingFailedEvent"/> record.
    /// </summary>
    public FileProcessingFailedEvent(
        FileReference fileReference,
        EdiTransactionType transactionType,
        CorrelationId correlationId,
        string errorMessage,
        string? errorCode = null,
        string? stackTrace = null,
        int retryCount = 0)
    {
        FileReference = fileReference ?? throw new ArgumentNullException(nameof(fileReference));
        TransactionType = transactionType ?? throw new ArgumentNullException(nameof(transactionType));
        CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
        ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
        ErrorCode = errorCode;
        StackTrace = stackTrace;
        RetryCount = retryCount;
        OccurredOn = DateTime.UtcNow;
    }
}
