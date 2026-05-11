using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace BatchOrchestration.Application.Commands.CreateBatch;

public record CreateBatchCommand : IRequest<Result<Guid>>
{
    public int ClearinghouseId { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public IList<string> FileNames { get; init; } = new List<string>();
    public int ConcurrencyLimit { get; init; } = 5;
}
