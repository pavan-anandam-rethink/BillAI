using System.Diagnostics;
using System.Threading.Channels;
using ClearingHouse.EdiProcessing.Domain.Entities;
using ClearingHouse.EdiProcessing.Domain.Enums;
using ClearingHouse.EdiProcessing.Domain.Interfaces;
using ClearingHouse.EdiProcessing.Domain.ValueObjects;
using ClearingHouse.SharedKernel.Domain.Interfaces;
using ClearingHouse.SharedKernel.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;

namespace ClearingHouse.EdiProcessing.Application.Pipelines;

/// <summary>
/// Stream-based EDI processing pipeline that parses, validates, and transforms segments
/// using bounded channels for backpressure control. Never buffers the entire file in memory.
/// </summary>
public sealed class EdiProcessingPipeline : IEdiProcessingPipeline
{
    private const int ChannelBoundedCapacity = 100;

    private readonly IEdiParser _parser;
    private readonly IEdiValidator _validator;
    private readonly IEdiTransformer _transformer;
    private readonly IRepository<EdiFile> _repository;
    private readonly ILogger<EdiProcessingPipeline> _logger;

    public EdiProcessingPipeline(
        IEdiParser parser,
        IEdiValidator validator,
        IEdiTransformer transformer,
        IRepository<EdiFile> repository,
        ILogger<EdiProcessingPipeline> logger)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _transformer = transformer ?? throw new ArgumentNullException(nameof(transformer));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ProcessingMetrics> ProcessFileAsync(
        Stream stream,
        EdiFile ediFile,
        CancellationToken cancellationToken)
    {
        using var activity = DiagnosticConfig.FileProcessingSource.StartActivity(
            "EdiProcessingPipeline.ProcessFile",
            ActivityKind.Internal);

        activity?.SetTag("edi.file_id", ediFile.Id.ToString());

        var stopwatch = Stopwatch.StartNew();
        var totalSegments = 0;
        var processedSegments = 0;
        var failedSegments = 0;

        var channel = Channel.CreateBounded<EdiSegment>(new BoundedChannelOptions(ChannelBoundedCapacity)
        {
            SingleWriter = true,
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        // Phase 1: Parsing — producer writes parsed segments into the channel
        ediFile.MarkParsing();
        await _repository.UpdateAsync(ediFile, cancellationToken);

        _logger.LogDebug("Starting parse phase for EdiFile {EdiFileId}", ediFile.Id);

        var producerTask = ProduceSegmentsAsync(
            channel.Writer,
            stream,
            ediFile,
            cancellationToken);

        // Phase 2 & 3: Validation + Transformation — consumer reads from channel
        ediFile.MarkValidating();

        _logger.LogDebug("Starting validate/transform phase for EdiFile {EdiFileId}", ediFile.Id);

        await foreach (var segment in channel.Reader.ReadAllAsync(cancellationToken))
        {
            totalSegments++;

            try
            {
                var validationResult = await _validator.ValidateSegmentAsync(segment, cancellationToken);

                segment.SetValidationResult(validationResult.Severity, validationResult.Errors);

                if (!validationResult.IsValid)
                {
                    if (validationResult.Severity >= ValidationSeverity.Critical)
                    {
                        failedSegments++;
                        ediFile.IncrementErrorCount();

                        _logger.LogError(
                            "Critical validation error on segment {SequenceNumber} in EdiFile {EdiFileId}: {Errors}",
                            segment.SequenceNumber,
                            ediFile.Id,
                            string.Join("; ", validationResult.Errors));
                        continue;
                    }

                    // Warning/Error severity — log and continue processing
                    _logger.LogWarning(
                        "Validation issue ({Severity}) on segment {SequenceNumber} in EdiFile {EdiFileId}: {Errors}",
                        validationResult.Severity,
                        segment.SequenceNumber,
                        ediFile.Id,
                        string.Join("; ", validationResult.Errors));
                }

                // Transform
                ediFile.MarkTransforming();
                var transformed = await _transformer.TransformAsync(segment, cancellationToken);
                segment.SetParsedContent(transformed.TransformedContent);

                processedSegments++;
                ediFile.IncrementProcessedSegments();
            }
            catch (Exception ex)
            {
                failedSegments++;
                ediFile.IncrementErrorCount();

                _logger.LogError(ex,
                    "Error processing segment {SequenceNumber} in EdiFile {EdiFileId}",
                    segment.SequenceNumber,
                    ediFile.Id);
            }
        }

        // Ensure producer completes (propagates any parsing exceptions)
        await producerTask;

        stopwatch.Stop();

        var durationMs = stopwatch.ElapsedMilliseconds;
        var throughput = durationMs > 0
            ? totalSegments / (durationMs / 1000.0)
            : 0.0;

        var metrics = new ProcessingMetrics(
            totalSegments,
            processedSegments,
            failedSegments,
            durationMs,
            throughput);

        activity?.SetTag("edi.total_segments", totalSegments);
        activity?.SetTag("edi.processed_segments", processedSegments);
        activity?.SetTag("edi.failed_segments", failedSegments);
        activity?.SetTag("edi.duration_ms", durationMs);

        _logger.LogInformation(
            "Pipeline completed for EdiFile {EdiFileId}: Total={Total}, Processed={Processed}, Failed={Failed}, Duration={DurationMs}ms, Throughput={Throughput:F1} seg/s",
            ediFile.Id,
            totalSegments,
            processedSegments,
            failedSegments,
            durationMs,
            throughput);

        return metrics;
    }

    private async Task ProduceSegmentsAsync(
        ChannelWriter<EdiSegment> writer,
        Stream stream,
        EdiFile ediFile,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var segment in _parser.ParseStreamAsync(
                stream, ediFile.EdiTransactionType, cancellationToken))
            {
                await writer.WriteAsync(segment, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during parsing phase for EdiFile {EdiFileId}", ediFile.Id);
            throw;
        }
        finally
        {
            writer.Complete();
        }
    }
}
