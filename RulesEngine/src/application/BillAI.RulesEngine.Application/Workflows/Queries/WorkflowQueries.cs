using BillAI.RulesEngine.Domain.Entities.Workflows;
using BillAI.RulesEngine.Domain.Enums;
using BillAI.RulesEngine.Shared.Models;
using MediatR;

namespace BillAI.RulesEngine.Application.Workflows.Queries;

/// <summary>Query to get a single workflow by ID.</summary>
public sealed record GetWorkflowByIdQuery(Guid WorkflowId, string TenantId) : IRequest<WorkflowDefinition?>;

/// <summary>Query to get a paginated list of workflows.</summary>
public sealed record GetWorkflowsQuery(
    string TenantId,
    int Page = 1,
    int PageSize = 20,
    WorkflowStatus? Status = null) : IRequest<PagedResult<WorkflowDefinition>>;

/// <summary>Query to get a workflow instance by ID.</summary>
public sealed record GetWorkflowInstanceQuery(Guid InstanceId, string TenantId) : IRequest<WorkflowInstance?>;

/// <summary>Query to get paginated workflow instances.</summary>
public sealed record GetWorkflowInstancesQuery(
    string TenantId,
    int Page = 1,
    int PageSize = 20,
    WorkflowInstanceStatus? Status = null,
    Guid? WorkflowId = null) : IRequest<PagedResult<WorkflowInstance>>;
