using ClearingHouse.SharedKernel.Domain.ValueObjects;

namespace ClearingHouse.SharedKernel.Domain.Events;

/// <summary>
/// Domain event raised when a new processing batch is created.
/// </summary>
public sealed record BatchCreatedEvent : IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the batch.
    /// </summary>
    public Guid BatchId { get; }

    /// <summary>
    /// Gets the clearinghouse associated with this batch.
    /// </summary>
    public ClearinghouseIdentifier ClearinghouseIdentifier { get; }

    /// <summary>
    /// Gets the EDI transaction type for files in this batch.
    /// </summary>
    public EdiTransactionType TransactionType { get; }

    /// <summary>
    /// Gets the number of files in this batch.
    /// </summary>
    public int FileCount { get; }

    /// <inheritdoc />
    public CorrelationId CorrelationId { get; }

    /// <inheritdoc />
    public DateTime OccurredOn { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchCreatedEvent"/> record.
    /// </summary>
    public BatchCreatedEvent(
        Guid batchId,
        ClearinghouseIdentifier clearinghouseIdentifier,
        EdiTransactionType transactionType,
        int fileCount,
        CorrelationId correlationId)
    {
        BatchId = batchId;
        ClearinghouseIdentifier = clearinghouseIdentifier ?? throw new ArgumentNullException(nameof(clearinghouseIdentifier));
        TransactionType = transactionType ?? throw new ArgumentNullException(nameof(transactionType));
        FileCount = fileCount;
        CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
        OccurredOn = DateTime.UtcNow;
    }
}
