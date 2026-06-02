using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models.Claims;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BillingService.LegacyAdapters.Claims;

/// <summary>
/// Handles <see cref="GetClaimHeadersQuery"/> by delegating to the existing
/// <see cref="IClaimService"/> domain service through the <see cref="IClaimCompatibilityFacade"/>.
/// This handler preserves the existing retrieval behavior while participating in the
/// MediatR pipeline (performance logging, cancellation propagation, future caching decorators).
/// Migration path: once Application owns DTO projections, this handler is replaced by one
/// that queries a read-model directly via <c>IBillingSqlConnectionFactory</c>.
/// </summary>
public sealed class GetClaimHeadersQueryHandler(
    IClaimCompatibilityFacade claimFacade,
    ILogger<GetClaimHeadersQueryHandler> logger)
    : IRequestHandler<GetClaimHeadersQuery, ClaimHeaderModelResponseModel>
{
    public async Task<ClaimHeaderModelResponseModel> Handle(
        GetClaimHeadersQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "GetClaimHeaders: accountInfoId={AccountInfoId} pageNumber={PageNumber} pageSize={PageSize}",
            request.Request.AccountInfoId,
            request.Request.PageNumber,
            request.Request.PageSize);

        return await claimFacade.GetClaimHeadersAsync(request.Request, cancellationToken)
            .ConfigureAwait(false);
    }
}
