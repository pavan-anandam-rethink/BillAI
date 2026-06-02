using BillingService.Domain.Models;
using MediatR;

namespace BillingService.LegacyAdapters.Claims.Commands;

/// <summary>
/// Handles <see cref="SaveClaimCommand"/> by delegating to the compatibility
/// facade that wraps the existing domain service. No business logic is changed.
/// </summary>
public sealed class SaveClaimCommandHandler(IClaimCompatibilityFacade claimFacade)
    : IRequestHandler<SaveClaimCommand, int>
{
    public Task<int> Handle(SaveClaimCommand request, CancellationToken cancellationToken)
    {
        return claimFacade.SaveClaimAsync(request.Model, cancellationToken);
    }
}
