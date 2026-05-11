using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace BatchOrchestration.Application.Commands.CompleteBatchItem;

public record CompleteBatchItemCommand : IRequest<Result>
{
    public Guid BatchId { get; init; }
    public Guid FileId { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}
