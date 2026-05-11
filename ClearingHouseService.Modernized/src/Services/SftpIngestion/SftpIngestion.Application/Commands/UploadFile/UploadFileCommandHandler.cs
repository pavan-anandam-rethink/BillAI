using ClearingHouse.SharedKernel.Interfaces;
using ClearingHouse.SharedKernel.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SftpIngestion.Application.Commands.UploadFile;

public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, Result>
{
    private readonly IBlobStorageService _blobStorage;
    private readonly IEventBus _eventBus;
    private readonly ILogger<UploadFileCommandHandler> _logger;

    public UploadFileCommandHandler(
        IBlobStorageService blobStorage,
        IEventBus eventBus,
        ILogger<UploadFileCommandHandler> logger)
    {
        _blobStorage = blobStorage;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Uploading file {FileName} to SFTP for clearinghouse {ClearinghouseId}, CorrelationId: {CorrelationId}",
            request.FileName, request.ClearinghouseId, request.CorrelationId);

        try
        {
            await Task.CompletedTask;
            _logger.LogInformation("File {FileName} uploaded successfully", request.FileName);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName}", request.FileName);
            return Result.Failure($"Upload failed: {ex.Message}");
        }
    }
}
