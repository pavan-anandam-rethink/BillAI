using MediatR;

namespace FileTracking.Application.Queries.GetFileTimeline;

public record GetFileTimelineQuery : IRequest<FileTimelineDto?>
{
    public Guid FileId { get; init; }
}

public record FileTimelineDto
{
    public Guid FileId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string CurrentStatus { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public IReadOnlyList<TimelineEventDto> Events { get; init; } = Array.Empty<TimelineEventDto>();
}

public record TimelineEventDto
{
    public string EventType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
}
