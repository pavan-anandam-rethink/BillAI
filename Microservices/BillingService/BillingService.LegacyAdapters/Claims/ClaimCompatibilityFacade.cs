using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using Rethink.Services.Common.Models;

namespace BillingService.LegacyAdapters.Claims;

public sealed class ClaimCompatibilityFacade(IClaimService claimService) : IClaimCompatibilityFacade
{
    public Task<ClaimHeaderModelResponseModel> GetClaimHeadersAsync(
        ClaimGetRequestSortFilterWithUserInfo model,
        CancellationToken cancellationToken = default)
    {
        return claimService.GetClaimHeadersAsync(model);
    }

    public Task<int> SaveClaimAsync(
        ClaimSaveModelWithUserInfo model,
        CancellationToken cancellationToken = default)
    {
        return claimService.SaveClaimAsync(model);
    }

    public Task<ActionResponse> GetClaimByIdentifierAsync(
        string claimIdentifier,
        int accountInfoId,
        CancellationToken cancellationToken = default)
    {
        return claimService.GetClaimByIdentifierAsync(claimIdentifier, accountInfoId);
    }
}
