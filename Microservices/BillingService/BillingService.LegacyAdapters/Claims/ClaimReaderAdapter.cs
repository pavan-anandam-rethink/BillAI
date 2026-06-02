using BillingService.Application.Abstractions.Claims;
using BillingService.Contracts.Claims;
using BillingService.SharedKernel.Primitives;

namespace BillingService.LegacyAdapters.Claims;

/// <summary>
/// Implements the Application-layer <see cref="IClaimReader"/> port by delegating to the
/// legacy <see cref="IClaimCompatibilityFacade"/>. This adapter translates the legacy
/// <c>ActionResponse</c> return into the clean <see cref="ClaimSummaryDto"/> contract
/// without changing any existing domain behavior.
/// </summary>
public sealed class ClaimReaderAdapter(IClaimCompatibilityFacade claimCompatibilityFacade)
    : IClaimReader
{
    public async Task<Result<ClaimSummaryDto>> GetByIdentifierAsync(
        string claimIdentifier,
        int accountInfoId,
        CancellationToken cancellationToken = default)
    {
        var response = await claimCompatibilityFacade
            .GetClaimByIdentifierAsync(claimIdentifier, accountInfoId, cancellationToken)
            .ConfigureAwait(false);

        if (response is null)
        {
            return Result<ClaimSummaryDto>.Failure(
                Error.NotFound("claim.not_found", $"Claim '{claimIdentifier}' not found for account {accountInfoId}."));
        }

        // Map the legacy ActionResponse to the clean DTO.
        // The raw payload is preserved so downstream consumers are not broken
        // while full CQRS read-model projection is built out.
        var dto = new ClaimSummaryDto
        {
            ClaimIdentifier = claimIdentifier,
            AccountInfoId   = accountInfoId,
            LegacyPayload   = response
        };

        return Result<ClaimSummaryDto>.Success(dto);
    }
}
