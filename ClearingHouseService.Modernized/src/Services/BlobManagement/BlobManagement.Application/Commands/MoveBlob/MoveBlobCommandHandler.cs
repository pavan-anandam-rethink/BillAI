using ClearingHouse.SharedKernel.Interfaces;
using ClearingHouse.SharedKernel.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BlobManagement.Application.Commands.MoveBlob;

public class MoveBlobCommandHandler : IRequestHandler<MoveBlobCommand, Result>
{
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<MoveBlobCommandHandler> _logger;

    public MoveBlobCommandHandler(IBlobStorageService blobStorage, ILogger<MoveBlobCommandHandler> logger)
    {
        _blobStorage = blobStorage;
        _logger = logger;
    }

    public async Task<Result> Handle(MoveBlobCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Moving blob {SourceBlob} to {DestBlob}", request.SourceBlobName, request.DestBlobName);

        try
        {
            await _blobStorage.MoveAsync(request.SourceContainer, request.SourceBlobName, request.DestContainer, request.DestBlobName, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move blob {SourceBlob}", request.SourceBlobName);
            return Result.Failure($"Move failed: {ex.Message}");
        }
    }
}
