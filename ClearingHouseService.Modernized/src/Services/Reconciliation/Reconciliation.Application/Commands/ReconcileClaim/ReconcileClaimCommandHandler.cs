using ClearingHouse.SharedKernel.Models;
using Reconciliation.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Reconciliation.Application.Commands.ReconcileClaim;

public class ReconcileClaimCommandHandler : IRequestHandler<ReconcileClaimCommand, Result>
{
    private readonly IReconciliationRepository _repository;
    private readonly ILogger<ReconcileClaimCommandHandler> _logger;

    public ReconcileClaimCommandHandler(IReconciliationRepository repository, ILogger<ReconcileClaimCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> Handle(ReconcileClaimCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reconciling claim {ClaimId}, CorrelationId: {CorrelationId}", request.ClaimId, request.CorrelationId);

        var existing = await _repository.GetByClaimIdAsync(request.ClaimId, cancellationToken);
        if (existing is not null)
        {
            existing.MatchWithResponse(request.ResponseFileId, "Matched");
            await _repository.UpdateAsync(existing, cancellationToken);
        }

        return Result.Success();
    }
}
