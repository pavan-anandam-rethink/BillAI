using BlobManagement.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BlobManagement.Application.Queries.GetBlobMetadata;

public class GetBlobMetadataQueryHandler : IRequestHandler<GetBlobMetadataQuery, BlobMetadataResponse?>
{
    private readonly IBlobFileRepository _repository;
    private readonly ILogger<GetBlobMetadataQueryHandler> _logger;

    public GetBlobMetadataQueryHandler(IBlobFileRepository repository, ILogger<GetBlobMetadataQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<BlobMetadataResponse?> Handle(GetBlobMetadataQuery request, CancellationToken cancellationToken)
    {
        var blob = await _repository.GetByIdAsync(request.BlobFileId, cancellationToken);
        if (blob is null) return null;

        return new BlobMetadataResponse
        {
            Id = blob.Id,
            ContainerName = blob.ContainerName,
            BlobName = blob.BlobName,
            FileSizeBytes = blob.FileSizeBytes,
            ContentHash = blob.ContentHash,
            ContentType = blob.ContentType,
            Status = blob.Status.ToString(),
            ArchivedAt = blob.ArchivedAt
        };
    }
}
