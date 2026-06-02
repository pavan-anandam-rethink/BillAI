using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using MediatR;
using Rethink.Services.Common.Models;

namespace BillingService.LegacyAdapters.Claims.Queries;

/// <summary>
/// CQRS read query that retrieves the paginated claim header list for the given
/// filter and sort criteria. Routed through the existing <see cref="IClaimCompatibilityFacade"/>
/// to preserve 100% existing behavior during incremental migration.
/// </summary>
public sealed record GetClaimHeadersQuery(ClaimGetRequestSortFilterWithUserInfo Request)
    : IRequest<ClaimHeaderModelResponseModel>;
