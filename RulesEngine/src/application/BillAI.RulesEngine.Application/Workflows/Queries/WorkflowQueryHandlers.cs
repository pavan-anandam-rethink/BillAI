using BillAI.RulesEngine.Application.Interfaces.Repositories;
using BillAI.RulesEngine.Domain.Entities.Workflows;
using BillAI.RulesEngine.Shared.Models;
using MediatR;

namespace BillAI.RulesEngine.Application.Workflows.Queries;

/// <summary>Handles <see cref="GetWorkflowByIdQuery"/>.</summary>
public sealed class GetWorkflowByIdQueryHandler(IWorkflowRepository workflowRepository)
    : IRequestHandler<GetWorkflowByIdQuery, WorkflowDefinition?>
{
    public Task<WorkflowDefinition?> Handle(GetWorkflowByIdQuery request, CancellationToken cancellationToken)
        => workflowRepository.GetByIdAsync(request.WorkflowId, request.TenantId, cancellationToken);
}

/// <summary>Handles <see cref="GetWorkflowsQuery"/>.</summary>
public sealed class GetWorkflowsQueryHandler(IWorkflowRepository workflowRepository)
    : IRequestHandler<GetWorkflowsQuery, PagedResult<WorkflowDefinition>>
{
    public Task<PagedResult<WorkflowDefinition>> Handle(GetWorkflowsQuery request, CancellationToken cancellationToken)
        => workflowRepository.GetAllAsync(
            request.TenantId,
            request.Page,
            request.PageSize,
            request.Status,
            cancellationToken);
}

/// <summary>Handles <see cref="GetWorkflowInstanceQuery"/>.</summary>
public sealed class GetWorkflowInstanceQueryHandler(IWorkflowInstanceRepository instanceRepository)
    : IRequestHandler<GetWorkflowInstanceQuery, WorkflowInstance?>
{
    public Task<WorkflowInstance?> Handle(GetWorkflowInstanceQuery request, CancellationToken cancellationToken)
        => instanceRepository.GetByIdAsync(request.InstanceId, request.TenantId, cancellationToken);
}

/// <summary>Handles <see cref="GetWorkflowInstancesQuery"/>.</summary>
public sealed class GetWorkflowInstancesQueryHandler(IWorkflowInstanceRepository instanceRepository)
    : IRequestHandler<GetWorkflowInstancesQuery, PagedResult<WorkflowInstance>>
{
    public Task<PagedResult<WorkflowInstance>> Handle(GetWorkflowInstancesQuery request, CancellationToken cancellationToken)
        => instanceRepository.GetAllAsync(
            request.TenantId,
            request.Page,
            request.PageSize,
            request.Status,
            request.WorkflowId,
            cancellationToken);
}
