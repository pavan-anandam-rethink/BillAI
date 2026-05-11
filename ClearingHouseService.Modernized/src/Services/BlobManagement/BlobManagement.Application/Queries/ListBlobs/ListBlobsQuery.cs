using MediatR;

namespace BlobManagement.Application.Queries.ListBlobs;

public record ListBlobsQuery : IRequest<IReadOnlyList<BlobSummary>>
{
    public string ContainerName { get; init; } = string.Empty;
}

public record BlobSummary
{
    public Guid Id { get; init; }
    public string BlobName { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string Status { get; init; } = string.Empty;
}
