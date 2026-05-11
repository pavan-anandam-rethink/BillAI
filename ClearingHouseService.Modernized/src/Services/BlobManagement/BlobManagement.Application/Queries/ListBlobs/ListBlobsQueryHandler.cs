using BlobManagement.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BlobManagement.Application.Queries.ListBlobs;

public class ListBlobsQueryHandler : IRequestHandler<ListBlobsQuery, IReadOnlyList<BlobSummary>>
{
    private readonly IBlobFileRepository _repository;
    private readonly ILogger<ListBlobsQueryHandler> _logger;

    public ListBlobsQueryHandler(IBlobFileRepository repository, ILogger<ListBlobsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BlobSummary>> Handle(ListBlobsQuery request, CancellationToken cancellationToken)
    {
        var blobs = await _repository.GetByContainerAsync(request.ContainerName, cancellationToken);
        return blobs.Select(b => new BlobSummary
        {
            Id = b.Id,
            BlobName = b.BlobName,
            FileSizeBytes = b.FileSizeBytes,
            Status = b.Status.ToString()
        }).ToList();
    }
}
