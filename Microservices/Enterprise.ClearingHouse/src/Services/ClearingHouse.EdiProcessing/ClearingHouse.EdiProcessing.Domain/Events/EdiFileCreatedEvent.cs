using ClearingHouse.SharedKernel.Domain.Events;
using ClearingHouse.SharedKernel.Domain.ValueObjects;

namespace ClearingHouse.EdiProcessing.Domain.Events;

/// <summary>
/// Domain event raised when a new EDI file is created for processing.
/// </summary>
/// <param name="EdiFileId">The unique identifier of the EDI file.</param>
/// <param name="FileName">The name of the EDI file.</param>
/// <param name="EdiTransactionType">The EDI transaction type.</param>
/// <param name="CorrelationId">The correlation identifier for tracing.</param>
/// <param name="OccurredOn">The date and time when the event occurred.</param>
public sealed record EdiFileCreatedEvent(
    Guid EdiFileId,
    string FileName,
    EdiTransactionType EdiTransactionType,
    CorrelationId CorrelationId,
    DateTime OccurredOn) : IDomainEvent;
