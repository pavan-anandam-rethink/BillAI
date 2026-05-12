using ClearingHouse.SharedKernel.Domain.Events;
using ClearingHouse.SharedKernel.Domain.ValueObjects;

namespace ClearingHouse.EdiProcessing.Domain.Events;

/// <summary>
/// Domain event raised when EDI file processing has failed.
/// </summary>
/// <param name="EdiFileId">The unique identifier of the EDI file.</param>
/// <param name="ErrorMessage">The error message describing the failure.</param>
/// <param name="CorrelationId">The correlation identifier for tracing.</param>
/// <param name="OccurredOn">The date and time when the event occurred.</param>
public sealed record EdiFileFailedEvent(
    Guid EdiFileId,
    string ErrorMessage,
    CorrelationId CorrelationId,
    DateTime OccurredOn) : IDomainEvent;
