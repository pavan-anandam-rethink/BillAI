using ClearingHouse.Contracts.Dtos;
using MediatR;

namespace BatchOrchestration.Application.Queries.GetBatchStatus;

public record GetBatchStatusQuery : IRequest<BatchStatusDto?>
{
    public Guid BatchId { get; init; }
}
