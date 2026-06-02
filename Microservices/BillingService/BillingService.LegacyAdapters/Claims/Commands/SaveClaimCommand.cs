using BillingService.Domain.Models;
using MediatR;

namespace BillingService.LegacyAdapters.Claims.Commands;

/// <summary>
/// CQRS write command that creates or updates a claim. Returns the resulting
/// claim ID (0 on failure). Delegated to the legacy domain service via the
/// compatibility facade to preserve existing business logic without change.
/// </summary>
public sealed record SaveClaimCommand(ClaimSaveModelWithUserInfo Model)
    : IRequest<int>;
