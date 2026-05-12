using System.Diagnostics;
using ClearingHouse.SharedKernel.Domain;
using ClearingHouse.SharedKernel.Messaging;
using ClearingHouse.SharedKernel.Observability;
using ClearingHouse.SharedKernel.Storage;
using EdiProcessing.Domain.Entities;
using EdiProcessing.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EdiProcessing.Application.Commands;

public class ProcessEdiFileCommandHandler : IRequestHandler<ProcessEdiFileCommand, Result>
{
    private readonly IEdiFileRepository _fileRepository;
    private readonly IBlobStorageService _blobStorage;
    private readonly IEnumerable<IEdiParser> _parsers;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ProcessEdiFileCommandHandler> _logger;

    public ProcessEdiFileCommandHandler(
        IEdiFileRepository fileRepository,
        IBlobStorageService blobStorage,
        IEnumerable<IEdiParser> parsers,
        IEventPublisher eventPublisher,
        ILogger<ProcessEdiFileCommandHandler> logger)
    {
        _fileRepository = fileRepository;
        _blobStorage = blobStorage;
        _parsers = parsers;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result> Handle(ProcessEdiFileCommand request, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity(TelemetryConstants.Activities.EdiProcess);
        activity?.SetTag(TelemetryConstants.Tags.ClearinghouseId, request.ClearinghouseId);
        activity?.SetTag(TelemetryConstants.Tags.FileType, request.FileType.ToString());
        activity?.SetTag(TelemetryConstants.Tags.CorrelationId, request.CorrelationId);

        var stopwatch = Stopwatch.StartNew();

        var ediFile = EdiFile.Create(request.FileName, request.BlobUri, request.FileType,
            request.ClearinghouseId, request.CorrelationId, 0, request.BatchId);

        await _fileRepository.AddAsync(ediFile, cancellationToken);

        try
        {
            ediFile.StartProcessing();
            await _fileRepository.UpdateAsync(ediFile, cancellationToken);

            var parser = _parsers.FirstOrDefault(p => p.SupportedType == request.FileType)
                ?? throw new InvalidOperationException($"No parser registered for EDI type: {request.FileType}");

            // Stream-based processing - never loads full file into memory
            var blobContainer = $"inbound-{request.ClearinghouseId.ToLowerInvariant()}";
            await using var stream = await _blobStorage.DownloadAsync(blobContainer, ExtractBlobName(request.BlobUri), cancellationToken);

            var context = new EdiParseContext(request.FileId, request.FileName, request.ClearinghouseId, request.CorrelationId, request.BatchId);
            var result = await parser.ParseAsync(stream, context, cancellationToken);

            ediFile.UpdateProgress(result.ProcessedSegments, result.TotalSegments);

            if (result.IsSuccess)
            {
                ediFile.Complete();
                DiagnosticsConfig.FilesProcessedCounter.Add(1,
                    new KeyValuePair<string, object?>(TelemetryConstants.Tags.ClearinghouseId, request.ClearinghouseId),
                    new KeyValuePair<string, object?>(TelemetryConstants.Tags.FileType, request.FileType.ToString()));
            }
            else
            {
                ediFile.Fail(string.Join("; ", result.Errors));
                DiagnosticsConfig.FilesFailedCounter.Add(1,
                    new KeyValuePair<string, object?>(TelemetryConstants.Tags.ClearinghouseId, request.ClearinghouseId));
            }

            await _fileRepository.UpdateAsync(ediFile, cancellationToken);

            await _eventPublisher.PublishAsync(new EdiFileProcessedIntegrationEvent
            {
                CorrelationId = request.CorrelationId,
                FileId = ediFile.Id,
                FileName = request.FileName,
                FileType = request.FileType.ToString(),
                ClearinghouseId = request.ClearinghouseId,
                IsSuccess = result.IsSuccess,
                TotalSegments = result.TotalSegments,
                ProcessedSegments = result.ProcessedSegments,
                ErrorCount = result.ErrorCount
            }, "edi-processed-topic", cancellationToken);

            stopwatch.Stop();
            DiagnosticsConfig.ProcessingDurationHistogram.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>(TelemetryConstants.Tags.FileType, request.FileType.ToString()));

            _logger.LogInformation("EDI file {FileName} processed in {ElapsedMs}ms. Success: {IsSuccess}, Segments: {Processed}/{Total}",
                request.FileName, stopwatch.ElapsedMilliseconds, result.IsSuccess, result.ProcessedSegments, result.TotalSegments);

            return result.IsSuccess ? Result.Success() : Result.Failure(string.Join("; ", result.Errors));
        }
        catch (Exception ex)
        {
            ediFile.Fail(ex.Message);
            await _fileRepository.UpdateAsync(ediFile, cancellationToken);
            _logger.LogError(ex, "Failed to process EDI file {FileName}", request.FileName);
            return Result.Failure(ex.Message);
        }
    }

    private static string ExtractBlobName(string blobUri)
    {
        if (Uri.TryCreate(blobUri, UriKind.Absolute, out var uri))
            return uri.AbsolutePath.TrimStart('/').Substring(uri.AbsolutePath.IndexOf('/', 1));
        return blobUri;
    }
}

public record EdiFileProcessedIntegrationEvent : IntegrationEvent
{
    public Guid FileId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public string ClearinghouseId { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public int TotalSegments { get; init; }
    public int ProcessedSegments { get; init; }
    public int ErrorCount { get; init; }
}
