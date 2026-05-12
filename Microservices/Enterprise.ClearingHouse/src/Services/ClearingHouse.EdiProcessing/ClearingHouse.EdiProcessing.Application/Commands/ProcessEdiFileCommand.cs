using ClearingHouse.EdiProcessing.Domain.Enums;
using ClearingHouse.EdiProcessing.Domain.ValueObjects;
using ClearingHouse.SharedKernel.Domain.Enums;
using ClearingHouse.SharedKernel.Domain.ValueObjects;
using MediatR;

namespace ClearingHouse.EdiProcessing.Application.Commands;

/// <summary>
/// Command to initiate EDI file processing through the full parsing, validation, and transformation pipeline.
/// </summary>
public sealed record ProcessEdiFileCommand(
    FileReference FileReference,
    EdiTransactionType EdiTransactionType,
    ClearinghouseType ClearinghouseType,
    CorrelationId CorrelationId,
    Guid? BatchId = null
) : IRequest<ProcessingResult>;

/// <summary>
/// Result of an EDI file processing operation.
/// </summary>
public sealed record ProcessingResult(
    Guid EdiFileId,
    EdiProcessingStatus Status,
    ProcessingMetrics Metrics,
    string? ErrorMessage);
