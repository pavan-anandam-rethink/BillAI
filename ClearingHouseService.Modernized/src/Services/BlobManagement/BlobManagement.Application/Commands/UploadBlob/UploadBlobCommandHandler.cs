using BlobManagement.Domain.Entities;
using BlobManagement.Domain.Interfaces;
using ClearingHouse.SharedKernel.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BlobManagement.Application.Commands.UploadBlob;

public class UploadBlobCommandHandler : IRequestHandler<UploadBlobCommand, Result<string>>
{
    private readonly IBlobFileRepository _repository;
    private readonly ILogger<UploadBlobCommandHandler> _logger;

    public UploadBlobCommandHandler(IBlobFileRepository repository, ILogger<UploadBlobCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(UploadBlobCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Recording blob upload {BlobName} in container {ContainerName}", request.BlobName, request.ContainerName);

        try
        {
            var blobFile = BlobFile.Create(request.ContainerName, request.BlobName, request.FileSizeBytes, request.ContentHash, request.ContentType);
            await _repository.AddAsync(blobFile, cancellationToken);
            return Result.Success(blobFile.Id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record blob upload {BlobName}", request.BlobName);
            return Result.Failure<string>($"Upload failed: {ex.Message}");
        }
    }
}
