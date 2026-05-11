using FileTracking.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FileTracking.Application.Queries.GetFileTimeline;

public class GetFileTimelineQueryHandler : IRequestHandler<GetFileTimelineQuery, FileTimelineDto?>
{
    private readonly IFileTrackingRepository _repository;
    private readonly ILogger<GetFileTimelineQueryHandler> _logger;

    public GetFileTimelineQueryHandler(IFileTrackingRepository repository, ILogger<GetFileTimelineQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<FileTimelineDto?> Handle(GetFileTimelineQuery request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetByFileIdAsync(request.FileId, cancellationToken);
        if (record is null) return null;

        return new FileTimelineDto
        {
            FileId = record.FileId,
            FileName = record.FileName,
            CurrentStatus = record.CurrentStatus.ToString(),
            CorrelationId = record.CorrelationId,
            Events = record.Timeline.Select(t => new TimelineEventDto
            {
                EventType = t.EventType,
                Description = t.Description,
                OccurredAt = t.OccurredAt
            }).ToList()
        };
    }
}
