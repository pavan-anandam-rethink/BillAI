using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using MediatR;
using Rethink.Services.Common.Models;

namespace BillingService.LegacyAdapters.Claims.Queries;

/// <summary>
/// Handles <see cref="GetClaimHeadersQuery"/> by delegating to the compatibility
/// facade that wraps the existing domain service. No business logic is changed.
/// </summary>
public sealed class GetClaimHeadersQueryHandler(IClaimCompatibilityFacade claimFacade)
    : IRequestHandler<GetClaimHeadersQuery, ClaimHeaderModelResponseModel>
{
    public Task<ClaimHeaderModelResponseModel> Handle(
        GetClaimHeadersQuery request,
        CancellationToken cancellationToken)
    {
        return claimFacade.GetClaimHeadersAsync(request.Request, cancellationToken);
    }
}
