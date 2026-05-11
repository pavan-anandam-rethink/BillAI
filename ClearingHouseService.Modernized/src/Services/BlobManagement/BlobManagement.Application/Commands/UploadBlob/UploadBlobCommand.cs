using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace BlobManagement.Application.Commands.UploadBlob;

public record UploadBlobCommand : IRequest<Result<string>>
{
    public string ContainerName { get; init; } = string.Empty;
    public string BlobName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string ContentHash { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
}
