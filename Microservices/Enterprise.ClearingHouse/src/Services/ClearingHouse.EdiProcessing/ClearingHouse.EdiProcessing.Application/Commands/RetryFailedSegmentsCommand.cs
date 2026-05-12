using ClearingHouse.SharedKernel.Domain.ValueObjects;
using MediatR;

namespace ClearingHouse.EdiProcessing.Application.Commands;

/// <summary>
/// Command to retry processing of specific failed segments within an EDI file.
/// </summary>
public sealed record RetryFailedSegmentsCommand(
    Guid EdiFileId,
    IReadOnlyList<int> SegmentSequences,
    CorrelationId CorrelationId
) : IRequest<RetryResult>;

/// <summary>
/// Result of a failed-segment retry operation.
/// </summary>
public sealed record RetryResult(
    int ReprocessedCount,
    int FailedCount,
    IReadOnlyList<string> Errors);
