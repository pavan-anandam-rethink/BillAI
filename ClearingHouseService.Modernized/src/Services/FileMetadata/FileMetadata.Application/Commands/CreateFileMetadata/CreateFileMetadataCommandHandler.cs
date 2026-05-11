using ClearingHouse.SharedKernel.Models;
using FileMetadata.Domain.Entities;
using FileMetadata.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FileMetadata.Application.Commands.CreateFileMetadata;

public class CreateFileMetadataCommandHandler : IRequestHandler<CreateFileMetadataCommand, Result<Guid>>
{
    private readonly IFileMetadataRepository _repository;
    private readonly ILogger<CreateFileMetadataCommandHandler> _logger;

    public CreateFileMetadataCommandHandler(IFileMetadataRepository repository, ILogger<CreateFileMetadataCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateFileMetadataCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating file metadata for {FileName}, CorrelationId: {CorrelationId}", request.FileName, request.CorrelationId);

        try
        {
            var record = FileMetadataRecord.Create(request.FileName, request.BlobUri, request.FileSizeBytes, request.ContentHash,
                request.ClearinghouseId, request.ClearinghouseName, request.CorrelationId);
            await _repository.AddAsync(record, cancellationToken);
            return Result.Success(record.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create file metadata for {FileName}", request.FileName);
            return Result.Failure<Guid>($"Failed: {ex.Message}");
        }
    }
}
