using ClearingHouse.Contracts.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using SftpIngestion.Domain.Interfaces;

namespace SftpIngestion.Application.Queries.GetIngestionStatus;

public class GetIngestionStatusQueryHandler : IRequestHandler<GetIngestionStatusQuery, FileMetadataDto?>
{
    private readonly IIngestedFileRepository _repository;
    private readonly ILogger<GetIngestionStatusQueryHandler> _logger;

    public GetIngestionStatusQueryHandler(IIngestedFileRepository repository, ILogger<GetIngestionStatusQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<FileMetadataDto?> Handle(GetIngestionStatusQuery request, CancellationToken cancellationToken)
    {
        var file = await _repository.GetByIdAsync(request.FileId, cancellationToken);
        if (file is null) return null;

        return new FileMetadataDto
        {
            FileId = file.Id,
            FileName = file.FileName,
            BlobUri = file.BlobUri,
            FileSizeBytes = file.FileSizeBytes,
            ContentHash = file.ContentHash,
            ClearinghouseName = file.ClearinghouseName,
            ClearinghouseId = file.ClearinghouseId,
            EdiTransactionType = file.TransactionType.HasValue ? (int)file.TransactionType : 0,
            Status = file.Status.ToString(),
            CorrelationId = file.CorrelationId,
            CreatedAt = file.CreatedAt,
            RetryCount = file.RetryCount,
            ErrorMessage = file.ErrorMessage
        };
    }
}
