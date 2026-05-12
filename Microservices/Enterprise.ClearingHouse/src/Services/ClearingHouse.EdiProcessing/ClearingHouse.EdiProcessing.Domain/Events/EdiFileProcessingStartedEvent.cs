using ClearingHouse.SharedKernel.Domain.Events;
using ClearingHouse.SharedKernel.Domain.ValueObjects;

namespace ClearingHouse.EdiProcessing.Domain.Events;

/// <summary>
/// Domain event raised when EDI file processing has started.
/// </summary>
/// <param name="EdiFileId">The unique identifier of the EDI file.</param>
/// <param name="CorrelationId">The correlation identifier for tracing.</param>
/// <param name="OccurredOn">The date and time when the event occurred.</param>
public sealed record EdiFileProcessingStartedEvent(
    Guid EdiFileId,
    CorrelationId CorrelationId,
    DateTime OccurredOn) : IDomainEvent;
