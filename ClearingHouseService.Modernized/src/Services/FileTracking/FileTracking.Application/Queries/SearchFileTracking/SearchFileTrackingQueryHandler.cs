using FileTracking.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FileTracking.Application.Queries.SearchFileTracking;

public class SearchFileTrackingQueryHandler : IRequestHandler<SearchFileTrackingQuery, IReadOnlyList<FileTrackingSummaryDto>>
{
    private readonly IFileTrackingRepository _repository;
    private readonly ILogger<SearchFileTrackingQueryHandler> _logger;

    public SearchFileTrackingQueryHandler(IFileTrackingRepository repository, ILogger<SearchFileTrackingQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FileTrackingSummaryDto>> Handle(SearchFileTrackingQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<FileTracking.Domain.Entities.FileTrackingRecord> records;

        if (!string.IsNullOrEmpty(request.CorrelationId))
            records = await _repository.GetByCorrelationIdAsync(request.CorrelationId, cancellationToken);
        else if (request.ClearinghouseId.HasValue)
            records = await _repository.GetByClearinghouseAsync(request.ClearinghouseId.Value, cancellationToken);
        else
            records = await _repository.GetAllAsync(cancellationToken);

        return records.Select(r => new FileTrackingSummaryDto
        {
            FileId = r.FileId,
            FileName = r.FileName,
            CurrentStatus = r.CurrentStatus.ToString(),
            ClearinghouseName = r.ClearinghouseName,
            CorrelationId = r.CorrelationId,
            LastStatusChange = r.LastStatusChange
        }).ToList();
    }
}
