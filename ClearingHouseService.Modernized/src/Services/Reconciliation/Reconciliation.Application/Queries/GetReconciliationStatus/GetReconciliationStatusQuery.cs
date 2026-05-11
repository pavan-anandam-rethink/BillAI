using MediatR;

namespace Reconciliation.Application.Queries.GetReconciliationStatus;

public record GetReconciliationStatusQuery : IRequest<ReconciliationStatusDto?>
{
    public string ClaimId { get; init; } = string.Empty;
}

public record ReconciliationStatusDto
{
    public Guid Id { get; init; }
    public string ClaimId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public DateTime? MatchedAt { get; init; }
    public string? ErrorMessage { get; init; }
}
