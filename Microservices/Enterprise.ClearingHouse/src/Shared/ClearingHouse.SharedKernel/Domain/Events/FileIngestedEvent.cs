using ClearingHouse.SharedKernel.Domain.ValueObjects;

namespace ClearingHouse.SharedKernel.Domain.Events;

/// <summary>
/// Domain event raised when a file has been successfully ingested from a clearinghouse.
/// </summary>
public sealed record FileIngestedEvent : IDomainEvent
{
    /// <summary>
    /// Gets the reference to the ingested file in blob storage.
    /// </summary>
    public FileReference FileReference { get; }

    /// <summary>
    /// Gets the clearinghouse from which the file was ingested.
    /// </summary>
    public ClearinghouseIdentifier ClearinghouseIdentifier { get; }

    /// <summary>
    /// Gets the EDI transaction type of the ingested file.
    /// </summary>
    public EdiTransactionType TransactionType { get; }

    /// <inheritdoc />
    public CorrelationId CorrelationId { get; }

    /// <inheritdoc />
    public DateTime OccurredOn { get; }

    /// <summary>
    /// Gets the UTC timestamp when the file was ingested.
    /// </summary>
    public DateTime IngestedAt { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileIngestedEvent"/> record.
    /// </summary>
    public FileIngestedEvent(
        FileReference fileReference,
        ClearinghouseIdentifier clearinghouseIdentifier,
        EdiTransactionType transactionType,
        CorrelationId correlationId)
    {
        FileReference = fileReference ?? throw new ArgumentNullException(nameof(fileReference));
        ClearinghouseIdentifier = clearinghouseIdentifier ?? throw new ArgumentNullException(nameof(clearinghouseIdentifier));
        TransactionType = transactionType ?? throw new ArgumentNullException(nameof(transactionType));
        CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
        OccurredOn = DateTime.UtcNow;
        IngestedAt = DateTime.UtcNow;
    }
}
