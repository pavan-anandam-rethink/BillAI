using ClearingHouse.SharedKernel.Domain.ValueObjects;

namespace ClearingHouse.SharedKernel.Domain.Events;

/// <summary>
/// Domain event raised when a file has been successfully processed.
/// </summary>
public sealed record FileProcessedEvent : IDomainEvent
{
    /// <summary>
    /// Gets the reference to the processed file.
    /// </summary>
    public FileReference FileReference { get; }

    /// <summary>
    /// Gets the EDI transaction type of the processed file.
    /// </summary>
    public EdiTransactionType TransactionType { get; }

    /// <inheritdoc />
    public CorrelationId CorrelationId { get; }

    /// <inheritdoc />
    public DateTime OccurredOn { get; }

    /// <summary>
    /// Gets the UTC timestamp when processing completed.
    /// </summary>
    public DateTime ProcessedAt { get; }

    /// <summary>
    /// Gets the total number of transactions found in the file.
    /// </summary>
    public int TransactionCount { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileProcessedEvent"/> record.
    /// </summary>
    public FileProcessedEvent(
        FileReference fileReference,
        EdiTransactionType transactionType,
        CorrelationId correlationId,
        int transactionCount)
    {
        FileReference = fileReference ?? throw new ArgumentNullException(nameof(fileReference));
        TransactionType = transactionType ?? throw new ArgumentNullException(nameof(transactionType));
        CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
        TransactionCount = transactionCount;
        OccurredOn = DateTime.UtcNow;
        ProcessedAt = DateTime.UtcNow;
    }
}
