using BillAI.RulesEngine.Application.Interfaces.Services;
using BillAI.RulesEngine.Application.Workflows.Commands;
using BillAI.RulesEngine.Application.Workflows.Queries;
using BillAI.RulesEngine.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillAI.RulesEngine.WebApi.Controllers;

/// <summary>
/// REST API for managing workflow definitions and instances.
/// </summary>
[ApiController]
[Route("api/workflows")]
[Produces("application/json")]
public sealed class WorkflowsController(IMediator mediator, ITenantService tenantService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetWorkflows(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] WorkflowStatus? status = null,
        CancellationToken ct = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        return Ok(await mediator.Send(new GetWorkflowsQuery(tenantId, page, pageSize, status), ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetWorkflow(Guid id, CancellationToken ct)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var workflow = await mediator.Send(new GetWorkflowByIdQuery(id, tenantId), ct);
        return workflow is null ? NotFound() : Ok(workflow);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWorkflow([FromBody] CreateWorkflowRequest request, CancellationToken ct)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var createdBy = User.Identity?.Name ?? "anonymous";
        var result = await mediator.Send(new CreateWorkflowCommand(
            tenantId, request.Name, request.Category, createdBy, request.Description, request.TimeoutMinutes), ct);
        if (result.IsFailure) return BadRequest(result.Errors);
        return CreatedAtAction(nameof(GetWorkflow), new { id = result.Value }, new { id = result.Value });
    }

    [HttpPut("{id:guid}/definition")]
    public async Task<IActionResult> UpdateDefinition(Guid id, [FromBody] UpdateWorkflowDefinitionRequest request, CancellationToken ct)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var updatedBy = User.Identity?.Name ?? "anonymous";
        var result = await mediator.Send(new UpdateWorkflowDefinitionCommand(
            id, tenantId, request.DefinitionJson, updatedBy, request.Description, request.TimeoutMinutes), ct);
        if (result.IsFailure) return BadRequest(result.Errors);
        return NoContent();
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> PublishWorkflow(Guid id, CancellationToken ct)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var publishedBy = User.Identity?.Name ?? "anonymous";
        var result = await mediator.Send(new PublishWorkflowCommand(id, tenantId, publishedBy), ct);
        if (result.IsFailure) return BadRequest(result.Errors);
        return NoContent();
    }

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> StartWorkflow(Guid id, [FromBody] StartWorkflowRequest? request, CancellationToken ct)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var startedBy = User.Identity?.Name ?? "anonymous";
        var result = await mediator.Send(new StartWorkflowCommand(
            id, tenantId, startedBy, request?.InputDataJson, request?.CorrelationId), ct);
        if (result.IsFailure) return BadRequest(result.Errors);
        return Ok(new { instanceId = result.Value });
    }

    // ─── Instances ────────────────────────────────────────────────────────────

    [HttpGet("instances")]
    public async Task<IActionResult> GetInstances(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] WorkflowInstanceStatus? status = null,
        [FromQuery] Guid? workflowId = null,
        CancellationToken ct = default)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        return Ok(await mediator.Send(new GetWorkflowInstancesQuery(tenantId, page, pageSize, status, workflowId), ct));
    }

    [HttpGet("instances/{instanceId:guid}")]
    public async Task<IActionResult> GetInstance(Guid instanceId, CancellationToken ct)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var instance = await mediator.Send(new GetWorkflowInstanceQuery(instanceId, tenantId), ct);
        return instance is null ? NotFound() : Ok(instance);
    }

    [HttpPost("instances/{instanceId:guid}/cancel")]
    public async Task<IActionResult> CancelInstance(Guid instanceId, CancellationToken ct)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var cancelledBy = User.Identity?.Name ?? "anonymous";
        var result = await mediator.Send(new CancelWorkflowInstanceCommand(instanceId, tenantId, cancelledBy), ct);
        if (result.IsFailure) return BadRequest(result.Errors);
        return NoContent();
    }

    [HttpPost("instances/{instanceId:guid}/resume")]
    public async Task<IActionResult> ResumeInstance(Guid instanceId, [FromBody] ResumeInstanceRequest? request, CancellationToken ct)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        var resumedBy = User.Identity?.Name ?? "anonymous";
        var result = await mediator.Send(new ResumeWorkflowInstanceCommand(
            instanceId, tenantId, resumedBy, request?.ResumeDataJson), ct);
        if (result.IsFailure) return BadRequest(result.Errors);
        return NoContent();
    }
}

public sealed record CreateWorkflowRequest(string Name, string Category, string? Description = null, int? TimeoutMinutes = null);
public sealed record UpdateWorkflowDefinitionRequest(string DefinitionJson, string? Description = null, int? TimeoutMinutes = null);
public sealed record StartWorkflowRequest(string? InputDataJson = null, string? CorrelationId = null);
public sealed record ResumeInstanceRequest(string? ResumeDataJson = null);
