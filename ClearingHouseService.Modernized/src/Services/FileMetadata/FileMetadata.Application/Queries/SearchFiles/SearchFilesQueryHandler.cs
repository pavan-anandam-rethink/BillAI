using ClearingHouse.Contracts.Dtos;
using FileMetadata.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FileMetadata.Application.Queries.SearchFiles;

public class SearchFilesQueryHandler : IRequestHandler<SearchFilesQuery, IReadOnlyList<FileMetadataDto>>
{
    private readonly IFileMetadataRepository _repository;
    private readonly ILogger<SearchFilesQueryHandler> _logger;

    public SearchFilesQueryHandler(IFileMetadataRepository repository, ILogger<SearchFilesQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FileMetadataDto>> Handle(SearchFilesQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<FileMetadata.Domain.Entities.FileMetadataRecord> records;

        if (!string.IsNullOrEmpty(request.CorrelationId))
        {
            var record = await _repository.GetByCorrelationIdAsync(request.CorrelationId, cancellationToken);
            records = record is not null ? new[] { record } : Array.Empty<FileMetadata.Domain.Entities.FileMetadataRecord>();
        }
        else if (request.ClearinghouseId.HasValue)
        {
            records = await _repository.GetByClearinghouseAsync(request.ClearinghouseId.Value, cancellationToken);
        }
        else
        {
            records = await _repository.GetAllAsync(cancellationToken);
        }

        return records.Select(r => new FileMetadataDto
        {
            FileId = r.Id,
            FileName = r.FileName,
            BlobUri = r.BlobUri,
            FileSizeBytes = r.FileSizeBytes,
            ContentHash = r.ContentHash,
            ClearinghouseName = r.ClearinghouseName,
            ClearinghouseId = r.ClearinghouseId,
            EdiTransactionType = r.TransactionType.HasValue ? (int)r.TransactionType : 0,
            Status = r.Status.ToString(),
            CorrelationId = r.CorrelationId,
            CreatedAt = r.CreatedAt,
            ProcessedAt = r.ProcessedAt,
            RetryCount = r.RetryCount,
            ErrorMessage = r.ErrorMessage
        }).ToList();
    }
}
