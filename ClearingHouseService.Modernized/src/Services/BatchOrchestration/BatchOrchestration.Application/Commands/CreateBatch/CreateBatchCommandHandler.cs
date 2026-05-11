using BatchOrchestration.Domain.Entities;
using BatchOrchestration.Domain.Enums;
using BatchOrchestration.Domain.Interfaces;
using ClearingHouse.SharedKernel.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BatchOrchestration.Application.Commands.CreateBatch;

public class CreateBatchCommandHandler : IRequestHandler<CreateBatchCommand, Result<Guid>>
{
    private readonly IBatchRepository _repository;
    private readonly ILogger<CreateBatchCommandHandler> _logger;

    public CreateBatchCommandHandler(IBatchRepository repository, ILogger<CreateBatchCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateBatchCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating batch for clearinghouse {ClearinghouseId} with {FileCount} files", request.ClearinghouseId, request.FileNames.Count);

        try
        {
            var batch = Batch.Create(request.ClearinghouseId, request.CorrelationId, BatchPriority.Normal, request.ConcurrencyLimit);
            for (int i = 0; i < request.FileNames.Count; i++)
            {
                batch.AddItem(Guid.NewGuid(), request.FileNames[i]);
            }

            await _repository.AddAsync(batch, cancellationToken);
            return Result.Success(batch.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create batch");
            return Result.Failure<Guid>($"Failed: {ex.Message}");
        }
    }
}
