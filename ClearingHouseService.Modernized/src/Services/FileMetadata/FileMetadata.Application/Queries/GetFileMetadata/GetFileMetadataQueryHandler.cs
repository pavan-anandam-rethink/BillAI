using ClearingHouse.Contracts.Dtos;
using FileMetadata.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FileMetadata.Application.Queries.GetFileMetadata;

public class GetFileMetadataQueryHandler : IRequestHandler<GetFileMetadataQuery, FileMetadataDto?>
{
    private readonly IFileMetadataRepository _repository;
    private readonly ILogger<GetFileMetadataQueryHandler> _logger;

    public GetFileMetadataQueryHandler(IFileMetadataRepository repository, ILogger<GetFileMetadataQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<FileMetadataDto?> Handle(GetFileMetadataQuery request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(request.FileId, cancellationToken);
        if (record is null) return null;

        return new FileMetadataDto
        {
            FileId = record.Id,
            FileName = record.FileName,
            BlobUri = record.BlobUri,
            FileSizeBytes = record.FileSizeBytes,
            ContentHash = record.ContentHash,
            ClearinghouseName = record.ClearinghouseName,
            ClearinghouseId = record.ClearinghouseId,
            EdiTransactionType = record.TransactionType.HasValue ? (int)record.TransactionType : 0,
            Status = record.Status.ToString(),
            CorrelationId = record.CorrelationId,
            CreatedAt = record.CreatedAt,
            ProcessedAt = record.ProcessedAt,
            RetryCount = record.RetryCount,
            ErrorMessage = record.ErrorMessage
        };
    }
}
