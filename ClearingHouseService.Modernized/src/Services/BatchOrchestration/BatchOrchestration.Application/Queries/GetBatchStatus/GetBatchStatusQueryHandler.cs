using BatchOrchestration.Domain.Interfaces;
using ClearingHouse.Contracts.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BatchOrchestration.Application.Queries.GetBatchStatus;

public class GetBatchStatusQueryHandler : IRequestHandler<GetBatchStatusQuery, BatchStatusDto?>
{
    private readonly IBatchRepository _repository;
    private readonly ILogger<GetBatchStatusQueryHandler> _logger;

    public GetBatchStatusQueryHandler(IBatchRepository repository, ILogger<GetBatchStatusQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<BatchStatusDto?> Handle(GetBatchStatusQuery request, CancellationToken cancellationToken)
    {
        var batch = await _repository.GetByIdAsync(request.BatchId, cancellationToken);
        if (batch is null) return null;

        return new BatchStatusDto
        {
            BatchId = batch.Id,
            Status = batch.Status.ToString(),
            TotalFiles = batch.TotalFiles,
            ProcessedFiles = batch.ProcessedFiles,
            FailedFiles = batch.FailedFiles,
            PendingFiles = batch.TotalFiles - batch.ProcessedFiles - batch.FailedFiles,
            CreatedAt = batch.CreatedAt,
            CompletedAt = batch.CompletedAt
        };
    }
}
