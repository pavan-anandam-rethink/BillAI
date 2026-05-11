using MediatR;

namespace EdiProcessing.Application.Queries.GetProcessingResult;

public record GetProcessingResultQuery : IRequest<ProcessingResultDto?>
{
    public Guid FileId { get; init; }
}

public record ProcessingResultDto
{
    public Guid FileId { get; init; }
    public Guid DocumentId { get; init; }
    public int TotalRecords { get; init; }
    public int SuccessfulRecords { get; init; }
    public int FailedRecords { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public TimeSpan ProcessingDuration { get; init; }
}
