using System.Diagnostics;
using ClearingHouse.EdiProcessing.Application.Commands;
using ClearingHouse.EdiProcessing.Domain.Entities;
using ClearingHouse.EdiProcessing.Domain.Enums;
using ClearingHouse.EdiProcessing.Domain.Events;
using ClearingHouse.EdiProcessing.Domain.Interfaces;
using ClearingHouse.EdiProcessing.Domain.ValueObjects;
using ClearingHouse.SharedKernel.Domain.Interfaces;
using ClearingHouse.SharedKernel.Infrastructure.Telemetry;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClearingHouse.EdiProcessing.Application.Handlers;

/// <summary>
/// Handles <see cref="ProcessEdiFileCommand"/> by orchestrating the full EDI file processing pipeline.
/// </summary>
public sealed class ProcessEdiFileCommandHandler : IRequestHandler<ProcessEdiFileCommand, ProcessingResult>
{
    private readonly IRepository<EdiFile> _repository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IEdiProcessingPipeline _pipeline;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ProcessEdiFileCommandHandler> _logger;

    public ProcessEdiFileCommandHandler(
        IRepository<EdiFile> repository,
        IBlobStorageService blobStorageService,
        IEdiProcessingPipeline pipeline,
        IEventPublisher eventPublisher,
        ILogger<ProcessEdiFileCommandHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ProcessingResult> Handle(ProcessEdiFileCommand request, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticConfig.FileProcessingSource.StartActivity(
            "ProcessEdiFile",
            ActivityKind.Consumer);

        activity?.SetTag("correlation.id", request.CorrelationId.Value);
        activity?.SetTag("edi.transaction_type", request.EdiTransactionType.Code);
        activity?.SetTag("edi.file_name", request.FileReference.FileName);

        _logger.LogInformation(
            "Starting EDI file processing for {FileName} with CorrelationId {CorrelationId}",
            request.FileReference.FileName,
            request.CorrelationId.Value);

        var ediFile = EdiFile.Create(
            request.CorrelationId,
            request.FileReference.FileName,
            request.EdiTransactionType,
            request.ClearinghouseType);

        ediFile.SetFileReference(request.FileReference);
        await _repository.AddAsync(ediFile, cancellationToken);

        Stream? stream = null;
        try
        {
            stream = await _blobStorageService.DownloadStreamAsync(
                request.FileReference.ContainerName,
                request.FileReference.BlobPath,
                cancellationToken);

            ediFile.StartProcessing();
            await _repository.UpdateAsync(ediFile, cancellationToken);

            var metrics = await _pipeline.ProcessFileAsync(stream, ediFile, cancellationToken);

            if (metrics.FailedSegments == 0)
            {
                ediFile.MarkCompleted(metrics);

                _logger.LogInformation(
                    "EDI file {EdiFileId} completed successfully. Processed {ProcessedSegments}/{TotalSegments} segments in {DurationMs}ms",
                    ediFile.Id,
                    metrics.ProcessedSegments,
                    metrics.TotalSegments,
                    metrics.ProcessingDurationMs);

                await _repository.UpdateAsync(ediFile, cancellationToken);
                await _eventPublisher.PublishAsync(
                    new EdiFileCompletedEvent(ediFile.Id, metrics, request.CorrelationId, DateTime.UtcNow),
                    cancellationToken);

                return new ProcessingResult(ediFile.Id, EdiProcessingStatus.Completed, metrics, null);
            }
            else if (metrics.ProcessedSegments > 0)
            {
                ediFile.MarkPartiallyCompleted(metrics);

                _logger.LogWarning(
                    "EDI file {EdiFileId} partially completed. Processed {ProcessedSegments}/{TotalSegments}, Failed {FailedSegments}",
                    ediFile.Id,
                    metrics.ProcessedSegments,
                    metrics.TotalSegments,
                    metrics.FailedSegments);

                await _repository.UpdateAsync(ediFile, cancellationToken);

                return new ProcessingResult(ediFile.Id, EdiProcessingStatus.PartiallyCompleted, metrics, null);
            }
            else
            {
                var errorMessage = "All segments failed processing";
                ediFile.MarkFailed(errorMessage);

                _logger.LogError(
                    "EDI file {EdiFileId} failed completely. {TotalSegments} segments all failed",
                    ediFile.Id,
                    metrics.TotalSegments);

                await _repository.UpdateAsync(ediFile, cancellationToken);
                await _eventPublisher.PublishAsync(
                    new EdiFileFailedEvent(ediFile.Id, errorMessage, request.CorrelationId, DateTime.UtcNow),
                    cancellationToken);

                return new ProcessingResult(ediFile.Id, EdiProcessingStatus.Failed, metrics, errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled error processing EDI file {EdiFileId} for CorrelationId {CorrelationId}",
                ediFile.Id,
                request.CorrelationId.Value);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            ediFile.MarkFailed(ex.Message);
            await _repository.UpdateAsync(ediFile, cancellationToken);

            await _eventPublisher.PublishAsync(
                new EdiFileFailedEvent(ediFile.Id, ex.Message, request.CorrelationId, DateTime.UtcNow),
                cancellationToken);

            var emptyMetrics = new ProcessingMetrics(0, 0, 0, 0, 0.0);
            return new ProcessingResult(ediFile.Id, EdiProcessingStatus.Failed, emptyMetrics, ex.Message);
        }
        finally
        {
            if (stream is not null)
            {
                await stream.DisposeAsync();
            }
        }
    }
}
