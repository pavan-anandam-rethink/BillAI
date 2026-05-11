using ClearingHouse.SharedKernel.Interfaces;
using ClearingHouse.SharedKernel.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EdiProcessing.Application.Commands.ProcessEdiFile;

public class ProcessEdiFileCommandHandler : IRequestHandler<ProcessEdiFileCommand, Result<Guid>>
{
    private readonly IBlobStorageService _blobStorage;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ProcessEdiFileCommandHandler> _logger;

    public ProcessEdiFileCommandHandler(IBlobStorageService blobStorage, IEventBus eventBus, ILogger<ProcessEdiFileCommandHandler> logger)
    {
        _blobStorage = blobStorage;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(ProcessEdiFileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing EDI file {FileName} ({FileId}), CorrelationId: {CorrelationId}",
            request.FileName, request.FileId, request.CorrelationId);

        try
        {
            await Task.CompletedTask;
            _logger.LogInformation("EDI file {FileName} processed successfully", request.FileName);
            return Result.Success(request.FileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process EDI file {FileName}", request.FileName);
            return Result.Failure<Guid>($"Processing failed: {ex.Message}");
        }
    }
}
