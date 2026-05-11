using MediatR;

namespace BlobManagement.Application.Queries.GetBlobMetadata;

public record GetBlobMetadataQuery : IRequest<BlobMetadataResponse?>
{
    public Guid BlobFileId { get; init; }
}

public record BlobMetadataResponse
{
    public Guid Id { get; init; }
    public string ContainerName { get; init; } = string.Empty;
    public string BlobName { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string ContentHash { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? ArchivedAt { get; init; }
}
