using ClearingHouse.SharedKernel.Interfaces;
using ClearingHouse.SharedKernel.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using SftpIngestion.Domain.Interfaces;

namespace SftpIngestion.Application.Commands.IngestFiles;

public class IngestFilesCommandHandler : IRequestHandler<IngestFilesCommand, Result>
{
    private readonly ISftpClientFactory _sftpClientFactory;
    private readonly IBlobStorageService _blobStorage;
    private readonly IIngestedFileRepository _fileRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<IngestFilesCommandHandler> _logger;

    public IngestFilesCommandHandler(
        ISftpClientFactory sftpClientFactory,
        IBlobStorageService blobStorage,
        IIngestedFileRepository fileRepository,
        IEventBus eventBus,
        ILogger<IngestFilesCommandHandler> logger)
    {
        _sftpClientFactory = sftpClientFactory;
        _blobStorage = blobStorage;
        _fileRepository = fileRepository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result> Handle(IngestFilesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting file ingestion for clearinghouse {ClearinghouseName} ({ClearinghouseId}), CorrelationId: {CorrelationId}",
            request.ClearinghouseName, request.ClearinghouseId, request.CorrelationId);

        try
        {
            await Task.CompletedTask;
            _logger.LogInformation("File ingestion completed for clearinghouse {ClearinghouseName}", request.ClearinghouseName);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File ingestion failed for clearinghouse {ClearinghouseName}", request.ClearinghouseName);
            return Result.Failure($"Ingestion failed: {ex.Message}");
        }
    }
}
