using BillingService.Application.Abstractions.Claims;
using BillingService.Contracts.Claims;
using BillingService.SharedKernel.Primitives;
using MediatR;

namespace BillingService.Application.Claims;

/// <summary>
/// CQRS query: retrieve a compact summary for a single claim by its business identifier.
/// This is the first read-side extraction from the legacy <c>ClaimController</c>; the
/// handler delegates to <see cref="IClaimReader"/> (implemented in LegacyAdapters) so
/// existing domain behavior is preserved with zero regression risk.
/// </summary>
public sealed record GetClaimByIdentifierQuery(
    string ClaimIdentifier,
    int AccountInfoId) : IRequest<Result<ClaimSummaryDto>>;

public sealed class GetClaimByIdentifierQueryHandler(IClaimReader claimReader)
    : IRequestHandler<GetClaimByIdentifierQuery, Result<ClaimSummaryDto>>
{
    public async Task<Result<ClaimSummaryDto>> Handle(
        GetClaimByIdentifierQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClaimIdentifier))
        {
            return Result<ClaimSummaryDto>.Failure(
                Error.Validation("claim.identifier.required", "Claim identifier is required."));
        }

        if (request.AccountInfoId <= 0)
        {
            return Result<ClaimSummaryDto>.Failure(
                Error.Validation("claim.accountinfoid.invalid", "AccountInfoId must be a positive integer."));
        }

        return await claimReader.GetByIdentifierAsync(
            request.ClaimIdentifier,
            request.AccountInfoId,
            cancellationToken).ConfigureAwait(false);
    }
}
