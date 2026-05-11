using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace FileMetadata.Application.Commands.UpdateFileStatus;

public record UpdateFileStatusCommand : IRequest<Result>
{
    public Guid FileId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}
