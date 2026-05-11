using MediatR;

namespace FileTracking.Application.Queries.SearchFileTracking;

public record SearchFileTrackingQuery : IRequest<IReadOnlyList<FileTrackingSummaryDto>>
{
    public string? CorrelationId { get; init; }
    public int? ClearinghouseId { get; init; }
}

public record FileTrackingSummaryDto
{
    public Guid FileId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string CurrentStatus { get; init; } = string.Empty;
    public string ClearinghouseName { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public DateTime? LastStatusChange { get; init; }
}
