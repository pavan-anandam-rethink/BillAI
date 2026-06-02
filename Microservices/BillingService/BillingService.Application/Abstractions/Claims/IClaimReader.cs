using BillingService.Contracts.Claims;
using BillingService.SharedKernel.Primitives;

namespace BillingService.Application.Abstractions.Claims;

/// <summary>
/// Read-side port for claim data. Implemented by the LegacyAdapters layer until a native
/// CQRS read model is available. Defined here (Application layer) so query handlers remain
/// decoupled from the Domain and LegacyAdapters implementation details.
/// </summary>
public interface IClaimReader
{
    /// <summary>
    /// Returns a lightweight summary for a single claim identified by its business identifier.
    /// Returns <see cref="Result{T}.Failure"/> with <c>Error.NotFound</c> when the claim
    /// does not exist or does not belong to the given account.
    /// </summary>
    Task<Result<ClaimSummaryDto>> GetByIdentifierAsync(
        string claimIdentifier,
        int accountInfoId,
        CancellationToken cancellationToken = default);
}
