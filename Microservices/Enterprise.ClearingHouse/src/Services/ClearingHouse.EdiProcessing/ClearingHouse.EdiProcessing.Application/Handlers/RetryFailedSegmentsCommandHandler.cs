using System.Diagnostics;
using ClearingHouse.EdiProcessing.Application.Commands;
using ClearingHouse.EdiProcessing.Domain.Entities;
using ClearingHouse.EdiProcessing.Domain.Enums;
using ClearingHouse.EdiProcessing.Domain.Interfaces;
using ClearingHouse.SharedKernel.Domain.Interfaces;
using ClearingHouse.SharedKernel.Infrastructure.Telemetry;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClearingHouse.EdiProcessing.Application.Handlers;

/// <summary>
/// Handles <see cref="RetryFailedSegmentsCommand"/> by re-validating and re-transforming
/// specific failed segments within an EDI file.
/// </summary>
public sealed class RetryFailedSegmentsCommandHandler : IRequestHandler<RetryFailedSegmentsCommand, RetryResult>
{
    private readonly IRepository<EdiFile> _repository;
    private readonly IEdiValidator _validator;
    private readonly IEdiTransformer _transformer;
    private readonly ILogger<RetryFailedSegmentsCommandHandler> _logger;

    public RetryFailedSegmentsCommandHandler(
        IRepository<EdiFile> repository,
        IEdiValidator validator,
        IEdiTransformer transformer,
        ILogger<RetryFailedSegmentsCommandHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _transformer = transformer ?? throw new ArgumentNullException(nameof(transformer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<RetryResult> Handle(RetryFailedSegmentsCommand request, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticConfig.FileProcessingSource.StartActivity(
            "RetryFailedSegments",
            ActivityKind.Consumer);

        activity?.SetTag("correlation.id", request.CorrelationId.Value);
        activity?.SetTag("edi.file_id", request.EdiFileId.ToString());
        activity?.SetTag("edi.segment_count", request.SegmentSequences.Count);

        _logger.LogInformation(
            "Retrying {SegmentCount} failed segments for EdiFile {EdiFileId}",
            request.SegmentSequences.Count,
            request.EdiFileId);

        var ediFile = await _repository.GetByIdAsync(request.EdiFileId, cancellationToken)
            ?? throw new InvalidOperationException($"EDI file {request.EdiFileId} not found");

        var reprocessedCount = 0;
        var failedCount = 0;
        var errors = new List<string>();

        var segmentLookup = ediFile.Segments
            .Where(s => request.SegmentSequences.Contains(s.SequenceNumber))
            .ToDictionary(s => s.SequenceNumber);

        foreach (var sequenceNumber in request.SegmentSequences)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!segmentLookup.TryGetValue(sequenceNumber, out var segment))
            {
                failedCount++;
                errors.Add($"Segment sequence {sequenceNumber} not found in EDI file {request.EdiFileId}");
                continue;
            }

            try
            {
                var validationResult = await _validator.ValidateSegmentAsync(segment, cancellationToken);

                if (!validationResult.IsValid && validationResult.Severity >= ValidationSeverity.Critical)
                {
                    failedCount++;
                    var errorMsg = $"Segment {sequenceNumber} failed re-validation: {string.Join("; ", validationResult.Errors)}";
                    errors.Add(errorMsg);

                    _logger.LogWarning(
                        "Segment {SequenceNumber} in EdiFile {EdiFileId} failed re-validation with severity {Severity}",
                        sequenceNumber,
                        request.EdiFileId,
                        validationResult.Severity);
                    continue;
                }

                segment.SetValidationResult(validationResult.Severity, validationResult.Errors);

                var transformed = await _transformer.TransformAsync(segment, cancellationToken);
                segment.SetParsedContent(transformed.TransformedContent);

                reprocessedCount++;

                _logger.LogDebug(
                    "Successfully reprocessed segment {SequenceNumber} in EdiFile {EdiFileId}",
                    sequenceNumber,
                    request.EdiFileId);
            }
            catch (Exception ex)
            {
                failedCount++;
                errors.Add($"Segment {sequenceNumber} retry error: {ex.Message}");

                _logger.LogError(ex,
                    "Error retrying segment {SequenceNumber} in EdiFile {EdiFileId}",
                    sequenceNumber,
                    request.EdiFileId);
            }
        }

        ediFile.UpdateMetrics(
            ediFile.TotalSegments,
            ediFile.ProcessedSegments + reprocessedCount,
            ediFile.ErrorCount - reprocessedCount);

        await _repository.UpdateAsync(ediFile, cancellationToken);

        activity?.SetTag("edi.retry.reprocessed", reprocessedCount);
        activity?.SetTag("edi.retry.failed", failedCount);

        _logger.LogInformation(
            "Retry completed for EdiFile {EdiFileId}: {Reprocessed} reprocessed, {Failed} failed",
            request.EdiFileId,
            reprocessedCount,
            failedCount);

        return new RetryResult(reprocessedCount, failedCount, errors);
    }
}
