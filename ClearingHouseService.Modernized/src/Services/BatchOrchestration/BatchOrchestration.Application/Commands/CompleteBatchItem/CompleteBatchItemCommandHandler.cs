using BatchOrchestration.Domain.Interfaces;
using ClearingHouse.SharedKernel.Interfaces;
using ClearingHouse.SharedKernel.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BatchOrchestration.Application.Commands.CompleteBatchItem;

public class CompleteBatchItemCommandHandler : IRequestHandler<CompleteBatchItemCommand, Result>
{
    private readonly IBatchRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CompleteBatchItemCommandHandler> _logger;

    public CompleteBatchItemCommandHandler(IBatchRepository repository, IEventBus eventBus, ILogger<CompleteBatchItemCommandHandler> logger)
    {
        _repository = repository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result> Handle(CompleteBatchItemCommand request, CancellationToken cancellationToken)
    {
        var batch = await _repository.GetByIdAsync(request.BatchId, cancellationToken);
        if (batch is null) return Result.Failure("Batch not found");

        if (request.IsSuccess)
            batch.RecordFileProcessed();
        else
            batch.RecordFileFailed();

        await _repository.UpdateAsync(batch, cancellationToken);
        return Result.Success();
    }
}
