using System.Diagnostics;
using ClearingHouse.SharedKernel.Domain.Interfaces;
using ClearingHouse.SharedKernel.Domain.ValueObjects;
using ClearingHouse.SftpIngestion.Application.Commands;
using ClearingHouse.SftpIngestion.Domain.Entities;
using ClearingHouse.SftpIngestion.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClearingHouse.SftpIngestion.Application.Handlers;

/// <summary>
/// Handles the <see cref="StartIngestionCommand"/> by orchestrating the SFTP polling workflow.
/// </summary>
public sealed class StartIngestionCommandHandler : IRequestHandler<StartIngestionCommand, StartIngestionResult>
{
    private static readonly ActivitySource ActivitySource = new("ClearingHouse.SftpIngestion");

    private readonly ILogger<StartIngestionCommandHandler> _logger;
    private readonly IClearinghousePlugin _clearinghousePlugin;
    private readonly ISftpConnectionPool _connectionPool;
    private readonly IRepository<SftpIngestionJob> _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartIngestionCommandHandler"/> class.
    /// </summary>
    public StartIngestionCommandHandler(
        ILogger<StartIngestionCommandHandler> logger,
        IClearinghousePlugin clearinghousePlugin,
        ISftpConnectionPool connectionPool,
        IRepository<SftpIngestionJob> repository,
        IEventPublisher eventPublisher,
        IMediator mediator)
    {
        _logger = logger;
        _clearinghousePlugin = clearinghousePlugin;
        _connectionPool = connectionPool;
        _repository = repository;
        _eventPublisher = eventPublisher;
        _mediator = mediator;
    }

    /// <inheritdoc />
    public async Task<StartIngestionResult> Handle(StartIngestionCommand request, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("StartIngestion");
        activity?.SetTag("clearinghouse.code", request.ClearinghouseIdentifier.Code);
        activity?.SetTag("correlation.id", request.CorrelationId.Value);

        _logger.LogInformation(
            "Starting ingestion for clearinghouse {ClearinghouseCode} with correlation {CorrelationId}",
            request.ClearinghouseIdentifier.Code,
            request.CorrelationId.Value);

        // Validate clearinghouse connection
        var isHealthy = await _clearinghousePlugin.ValidateConnectionAsync(cancellationToken);
        if (!isHealthy)
        {
            _logger.LogWarning(
                "Clearinghouse {ClearinghouseCode} connection validation failed",
                request.ClearinghouseIdentifier.Code);

            throw new InvalidOperationException(
                $"Cannot connect to clearinghouse {request.ClearinghouseIdentifier.Code}. Connection validation failed.");
        }

        // Create ingestion job
        var job = SftpIngestionJob.Create(
            request.ClearinghouseIdentifier,
            "*/5 * * * *", // Default schedule
            request.CorrelationId);

        await _repository.AddAsync(job, cancellationToken);

        job.StartPolling();

        try
        {
            // Download files from clearinghouse
            var downloadedFiles = await _clearinghousePlugin.DownloadFilesAsync(
                transactionType: null,
                cancellationToken: cancellationToken);

            job.RecordFilesDiscovered(downloadedFiles.Count);

            _logger.LogInformation(
                "Discovered {FileCount} files for clearinghouse {ClearinghouseCode}",
                downloadedFiles.Count,
                request.ClearinghouseIdentifier.Code);

            if (downloadedFiles.Count == 0)
            {
                job.Complete();
                await _repository.UpdateAsync(job, cancellationToken);
                return new StartIngestionResult(job.Id, "Completed - no files found");
            }

            // Process each discovered file
            job.StartUploading();
            foreach (var file in downloadedFiles)
            {
                await using (file.Content)
                {
                    var processCommand = new ProcessDiscoveredFileCommand(
                        FileName: file.FileName,
                        FileSize: file.Content.Length,
                        ClearinghouseIdentifier: request.ClearinghouseIdentifier,
                        CorrelationId: request.CorrelationId,
                        SftpConnectionDetails: SftpIngestion.Domain.ValueObjects.SftpConnectionDetails.Create(
                            host: "configured-via-options",
                            port: 22,
                            username: "configured-via-options",
                            encryptedPassword: string.Empty,
                            downloadDirectory: "/inbound"),
                        IngestionJobId: job.Id);

                    var result = await _mediator.Send(processCommand, cancellationToken);
                    if (result.Success)
                    {
                        job.RecordFileProcessed();
                    }
                }
            }

            // Publish domain events
            job.StartPublishing();
            foreach (var domainEvent in job.DomainEvents)
            {
                await _eventPublisher.PublishAsync(domainEvent, cancellationToken);
            }

            job.Complete();
            await _repository.UpdateAsync(job, cancellationToken);

            activity?.SetTag("files.processed", job.FilesProcessed);
            _logger.LogInformation(
                "Ingestion completed for clearinghouse {ClearinghouseCode}. Processed {FilesProcessed}/{FilesDiscovered} files",
                request.ClearinghouseIdentifier.Code,
                job.FilesProcessed,
                job.FilesDiscovered);

            return new StartIngestionResult(job.Id, $"Completed - {job.FilesProcessed} files processed");
        }
        catch (OperationCanceledException)
        {
            job.TimedOut();
            await _repository.UpdateAsync(job, cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Ingestion failed for clearinghouse {ClearinghouseCode}",
                request.ClearinghouseIdentifier.Code);

            job.Fail(ex.Message);
            await _repository.UpdateAsync(job, cancellationToken);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
