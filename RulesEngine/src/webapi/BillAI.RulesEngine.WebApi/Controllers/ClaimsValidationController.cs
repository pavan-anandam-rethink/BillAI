using BillAI.RulesEngine.Application.Claims.Commands;
using BillAI.RulesEngine.Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BillAI.RulesEngine.WebApi.Controllers;

/// <summary>
/// REST API for healthcare claim validation.
/// </summary>
[ApiController]
[Route("api/rules")]
[Produces("application/json")]
public sealed class ClaimsValidationController(IMediator mediator, ITenantService tenantService) : ControllerBase
{
    /// <summary>
    /// Validates a healthcare claim JSON against all published rules for the tenant.
    /// </summary>
    /// <remarks>
    /// POST /api/rules/validateClaim
    ///
    /// The claim payload is validated against the tenant's active published rules.
    /// Returns a full decision trace, audit ID, passed rules, failed rules, and warnings.
    /// </remarks>
    [HttpPost("validateClaim")]
    public async Task<IActionResult> ValidateClaim([FromBody] ValidateClaimRequest request, CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.ClaimJson))
            return BadRequest("Claim JSON is required.");

        var tenantId = tenantService.GetCurrentTenantId();
        var requestedBy = User.Identity?.Name ?? "anonymous";

        var result = await mediator.Send(
            new ValidateClaimCommand(
                tenantId,
                request.ClaimId ?? Guid.NewGuid().ToString(),
                request.ClaimJson,
                requestedBy,
                request.Category),
            ct);

        if (result.IsFailure) return BadRequest(result.Errors);

        return Ok(result.Value);
    }
}

/// <summary>Request payload for the validateClaim endpoint.</summary>
public sealed record ValidateClaimRequest(
    string ClaimJson,
    string? ClaimId = null,
    string? Category = null);
