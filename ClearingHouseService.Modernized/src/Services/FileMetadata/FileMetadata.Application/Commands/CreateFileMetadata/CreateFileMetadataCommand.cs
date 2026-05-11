using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace FileMetadata.Application.Commands.CreateFileMetadata;

public record CreateFileMetadataCommand : IRequest<Result<Guid>>
{
    public string FileName { get; init; } = string.Empty;
    public string BlobUri { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string ContentHash { get; init; } = string.Empty;
    public int ClearinghouseId { get; init; }
    public string ClearinghouseName { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
}
