using BillAI.RulesEngine.Domain.Enums;
using BillAI.RulesEngine.Shared.Models;
using MediatR;

namespace BillAI.RulesEngine.Application.Workflows.Commands;

/// <summary>Command to create a new draft workflow.</summary>
public sealed record CreateWorkflowCommand(
    string TenantId,
    string Name,
    string Category,
    string CreatedBy,
    string? Description = null,
    int? TimeoutMinutes = null) : IRequest<Result<Guid>>;

/// <summary>Command to publish a workflow.</summary>
public sealed record PublishWorkflowCommand(
    Guid WorkflowId,
    string TenantId,
    string PublishedBy) : IRequest<Result>;

/// <summary>Command to update a workflow's definition JSON.</summary>
public sealed record UpdateWorkflowDefinitionCommand(
    Guid WorkflowId,
    string TenantId,
    string DefinitionJson,
    string UpdatedBy,
    string? Description = null,
    int? TimeoutMinutes = null) : IRequest<Result>;

/// <summary>Command to start a workflow execution.</summary>
public sealed record StartWorkflowCommand(
    Guid WorkflowId,
    string TenantId,
    string StartedBy,
    string? InputDataJson = null,
    string? CorrelationId = null) : IRequest<Result<Guid>>;

/// <summary>Command to cancel a running workflow instance.</summary>
public sealed record CancelWorkflowInstanceCommand(
    Guid InstanceId,
    string TenantId,
    string CancelledBy) : IRequest<Result>;

/// <summary>Command to resume a suspended workflow instance.</summary>
public sealed record ResumeWorkflowInstanceCommand(
    Guid InstanceId,
    string TenantId,
    string ResumedBy,
    string? ResumeDataJson = null) : IRequest<Result>;
