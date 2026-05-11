using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace Reconciliation.Application.Commands.ReconcileClaim;

public record ReconcileClaimCommand : IRequest<Result>
{
    public string ClaimId { get; init; } = string.Empty;
    public Guid SubmissionFileId { get; init; }
    public Guid ResponseFileId { get; init; }
    public int ClearinghouseId { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}
