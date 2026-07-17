using BillAI.RulesEngine.Application.Interfaces.Services;
using BillAI.RulesEngine.Application.Rules.Commands;
using BillAI.RulesEngine.Application.Rules.Queries;
using BillAI.RulesEngine.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillAI.RulesEngine.WebApi.Controllers;

/// <summary>
/// REST API for managing rule definitions.
/// </summary>
[ApiController]
[Route("api/rules")]
[Produces("application/json")]
public sealed class RulesController(IMediator mediator, ITenantService tenantService) : ControllerBase
{
    // ─── GET /api/rules ────────────────────────────────────────────────────────

    /// <summary>Returns a paginated list of rules for the current tenant.</summary>
    [HttpGet]
    public async Task<IActionResult> GetRules(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] RuleStatus? status = null,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var result = await mediator.Send(new GetRulesQuery(tenantId, page, pageSize, status, category, search), ct);
        return Ok(result);
    }

    // ─── GET /api/rules/{id} ───────────────────────────────────────────────────

    /// <summary>Returns a single rule by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRule(Guid id, CancellationToken ct)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var rule = await mediator.Send(new GetRuleByIdQuery(id, tenantId), ct);
        return rule is null ? NotFound() : Ok(rule);
    }

    // ─── GET /api/rules/{id}/versions ─────────────────────────────────────────

    /// <summary>Returns the version history of a rule.</summary>
    [HttpGet("{id:guid}/versions")]
    public async Task<IActionResult> GetVersions(Guid id, CancellationToken ct)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var versions = await mediator.Send(new GetRuleVersionsQuery(id, tenantId), ct);
        return Ok(versions);
    }

    // ─── POST /api/rules ──────────────────────────────────────────────────────

    /// <summary>Creates a new draft rule.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateRule([FromBody] CreateRuleRequest request, CancellationToken ct)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var createdBy = User.Identity?.Name ?? "anonymous";
        var result = await mediator.Send(new CreateRuleCommand(
            tenantId,
            request.Name,
            request.Category,
            request.RuleExpression,
            createdBy,
            request.Description,
            request.Priority,
            request.Severity,
            request.FailureMessage,
            request.EffectiveFrom,
            request.ExpiresAt), ct);

        if (result.IsFailure) return BadRequest(result.Errors);
        return CreatedAtAction(nameof(GetRule), new { id = result.Value }, new { id = result.Value });
    }

    // ─── PUT /api/rules/{id} ──────────────────────────────────────────────────

    /// <summary>Updates an existing rule.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRule(Guid id, [FromBody] UpdateRuleRequest request, CancellationToken ct)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var updatedBy = User.Identity?.Name ?? "anonymous";
        var result = await mediator.Send(new UpdateRuleCommand(
            id,
            tenantId,
            request.Name,
            request.Category,
            request.RuleExpression,
            updatedBy,
            request.Description,
            request.Priority,
            request.Severity,
            request.FailureMessage,
            request.EffectiveFrom,
            request.ExpiresAt,
            request.DecisionTableJson), ct);

        if (result.IsFailure) return BadRequest(result.Errors);
        return NoContent();
    }

    // ─── POST /api/rules/{id}/publish ─────────────────────────────────────────

    /// <summary>Publishes a rule, making it active.</summary>
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> PublishRule(Guid id, CancellationToken ct)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var publishedBy = User.Identity?.Name ?? "anonymous";
        var result = await mediator.Send(new PublishRuleCommand(id, tenantId, publishedBy), ct);
        if (result.IsFailure) return BadRequest(result.Errors);
        return NoContent();
    }

    // ─── POST /api/rules/{id}/archive ─────────────────────────────────────────

    /// <summary>Archives a rule.</summary>
    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> ArchiveRule(Guid id, CancellationToken ct)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var archivedBy = User.Identity?.Name ?? "anonymous";
        var result = await mediator.Send(new ArchiveRuleCommand(id, tenantId, archivedBy), ct);
        if (result.IsFailure) return BadRequest(result.Errors);
        return NoContent();
    }

    // ─── POST /api/rules/{id}/rollback ────────────────────────────────────────

    /// <summary>Rolls back a rule to a specific version.</summary>
    [HttpPost("{id:guid}/rollback")]
    public async Task<IActionResult> RollbackRule(Guid id, [FromBody] RollbackRuleRequest request, CancellationToken ct)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var rolledBackBy = User.Identity?.Name ?? "anonymous";
        var result = await mediator.Send(new RollbackRuleCommand(id, tenantId, request.TargetVersion, rolledBackBy), ct);
        if (result.IsFailure) return BadRequest(result.Errors);
        return NoContent();
    }

    // ─── PATCH /api/rules/{id}/toggle ─────────────────────────────────────────

    /// <summary>Enables or disables a rule.</summary>
    [HttpPatch("{id:guid}/toggle")]
    public async Task<IActionResult> ToggleRule(Guid id, [FromBody] ToggleRuleRequest request, CancellationToken ct)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var updatedBy = User.Identity?.Name ?? "anonymous";
        var result = await mediator.Send(new ToggleRuleCommand(id, tenantId, request.Enable, updatedBy), ct);
        if (result.IsFailure) return BadRequest(result.Errors);
        return NoContent();
    }

    // ─── DELETE /api/rules/{id} ───────────────────────────────────────────────

    /// <summary>Permanently deletes a rule.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRule(Guid id, CancellationToken ct)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var deletedBy = User.Identity?.Name ?? "anonymous";
        var result = await mediator.Send(new DeleteRuleCommand(id, tenantId, deletedBy), ct);
        if (result.IsFailure) return BadRequest(result.Errors);
        return NoContent();
    }
}

// ─── Request models ──────────────────────────────────────────────────────────

public sealed record CreateRuleRequest(
    string Name,
    string Category,
    string RuleExpression,
    string? Description = null,
    int Priority = 100,
    RuleSeverity Severity = RuleSeverity.Error,
    string? FailureMessage = null,
    DateTimeOffset? EffectiveFrom = null,
    DateTimeOffset? ExpiresAt = null);

public sealed record UpdateRuleRequest(
    string Name,
    string Category,
    string RuleExpression,
    string? Description = null,
    int Priority = 100,
    RuleSeverity Severity = RuleSeverity.Error,
    string? FailureMessage = null,
    DateTimeOffset? EffectiveFrom = null,
    DateTimeOffset? ExpiresAt = null,
    string? DecisionTableJson = null);

public sealed record RollbackRuleRequest(int TargetVersion);

public sealed record ToggleRuleRequest(bool Enable);
