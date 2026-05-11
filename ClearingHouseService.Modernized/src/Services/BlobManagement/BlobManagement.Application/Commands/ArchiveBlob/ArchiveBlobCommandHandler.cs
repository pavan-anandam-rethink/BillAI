using BlobManagement.Domain.Interfaces;
using ClearingHouse.SharedKernel.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BlobManagement.Application.Commands.ArchiveBlob;

public class ArchiveBlobCommandHandler : IRequestHandler<ArchiveBlobCommand, Result>
{
    private readonly IBlobFileRepository _repository;
    private readonly ILogger<ArchiveBlobCommandHandler> _logger;

    public ArchiveBlobCommandHandler(IBlobFileRepository repository, ILogger<ArchiveBlobCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> Handle(ArchiveBlobCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Archiving blob {BlobFileId}", request.BlobFileId);

        var blobFile = await _repository.GetByIdAsync(request.BlobFileId, cancellationToken);
        if (blobFile is null)
            return Result.Failure("Blob file not found");

        blobFile.Archive();
        await _repository.UpdateAsync(blobFile, cancellationToken);
        return Result.Success();
    }
}
