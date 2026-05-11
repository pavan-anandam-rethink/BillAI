using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace BlobManagement.Application.Commands.MoveBlob;

public record MoveBlobCommand : IRequest<Result>
{
    public string SourceContainer { get; init; } = string.Empty;
    public string SourceBlobName { get; init; } = string.Empty;
    public string DestContainer { get; init; } = string.Empty;
    public string DestBlobName { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
}
