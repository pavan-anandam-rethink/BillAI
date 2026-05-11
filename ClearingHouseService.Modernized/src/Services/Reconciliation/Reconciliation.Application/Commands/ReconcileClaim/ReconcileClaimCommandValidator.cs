using FluentValidation;

namespace Reconciliation.Application.Commands.ReconcileClaim;

public class ReconcileClaimCommandValidator : AbstractValidator<ReconcileClaimCommand>
{
    public ReconcileClaimCommandValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty().WithMessage("ClaimId is required");
        RuleFor(x => x.CorrelationId).NotEmpty().WithMessage("CorrelationId is required");
    }
}
