using Reconciliation.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Reconciliation.Application.Queries.GetReconciliationStatus;

public class GetReconciliationStatusQueryHandler : IRequestHandler<GetReconciliationStatusQuery, ReconciliationStatusDto?>
{
    private readonly IReconciliationRepository _repository;
    private readonly ILogger<GetReconciliationStatusQueryHandler> _logger;

    public GetReconciliationStatusQueryHandler(IReconciliationRepository repository, ILogger<GetReconciliationStatusQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ReconciliationStatusDto?> Handle(GetReconciliationStatusQuery request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetByClaimIdAsync(request.ClaimId, cancellationToken);
        if (record is null) return null;

        return new ReconciliationStatusDto
        {
            Id = record.Id,
            ClaimId = record.ClaimId,
            Status = record.Status,
            CorrelationId = record.CorrelationId,
            MatchedAt = record.MatchedAt,
            ErrorMessage = record.ErrorMessage
        };
    }
}
