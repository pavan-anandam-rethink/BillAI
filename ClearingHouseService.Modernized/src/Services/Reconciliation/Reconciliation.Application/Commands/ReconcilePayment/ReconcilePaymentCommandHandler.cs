using ClearingHouse.SharedKernel.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Reconciliation.Application.Commands.ReconcilePayment;

public class ReconcilePaymentCommandHandler : IRequestHandler<ReconcilePaymentCommand, Result>
{
    private readonly ILogger<ReconcilePaymentCommandHandler> _logger;

    public ReconcilePaymentCommandHandler(ILogger<ReconcilePaymentCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<Result> Handle(ReconcilePaymentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reconciling payment for claim {ClaimId}", request.ClaimId);
        await Task.CompletedTask;
        return Result.Success();
    }
}
