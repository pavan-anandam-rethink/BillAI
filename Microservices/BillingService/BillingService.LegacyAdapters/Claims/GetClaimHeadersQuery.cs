using BillingService.Domain.Models.Claims;
using MediatR;

namespace BillingService.LegacyAdapters.Claims;

/// <summary>
/// CQRS read query: retrieves a paginated, sorted, and filtered set of claim headers.
/// During the incremental migration phase this query lives in <c>BillingService.LegacyAdapters</c>
/// because the request and response types are still Domain-owned.
/// When the Application layer owns its own DTO projections the handler will move to
/// <c>BillingService.Application</c> and delegate to a true read-model repository.
/// </summary>
public sealed record GetClaimHeadersQuery(
    ClaimGetRequestSortFilterWithUserInfo Request)
    : IRequest<ClaimHeaderModelResponseModel>;
