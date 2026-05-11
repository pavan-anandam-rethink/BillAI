using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace FileTracking.Application.Commands.RecordFileEvent;

public record RecordFileEventCommand : IRequest<Result>
{
    public Guid FileId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public int ClearinghouseId { get; init; }
    public string ClearinghouseName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
}
