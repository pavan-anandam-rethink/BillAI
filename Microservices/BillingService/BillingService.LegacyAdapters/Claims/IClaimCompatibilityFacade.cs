using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using Rethink.Services.Common.Models;

namespace BillingService.LegacyAdapters.Claims;

public interface IClaimCompatibilityFacade
{
    Task<ClaimHeaderModelResponseModel> GetClaimHeadersAsync(
        ClaimGetRequestSortFilterWithUserInfo model,
        CancellationToken cancellationToken = default);

    Task<int> SaveClaimAsync(
        ClaimSaveModelWithUserInfo model,
        CancellationToken cancellationToken = default);

    Task<ActionResponse> GetClaimByIdentifierAsync(
        string claimIdentifier,
        int accountInfoId,
        CancellationToken cancellationToken = default);
}
