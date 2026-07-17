using BillAI.RulesEngine.Application.Interfaces.Repositories;
using BillAI.RulesEngine.Application.Interfaces.Services;
using BillAI.RulesEngine.Domain.Entities.Workflows;
using BillAI.RulesEngine.Domain.Exceptions;
using BillAI.RulesEngine.Shared.Models;
using MediatR;

namespace BillAI.RulesEngine.Application.Workflows.Commands;

/// <summary>Handles <see cref="CreateWorkflowCommand"/>.</summary>
public sealed class CreateWorkflowCommandHandler(IWorkflowRepository workflowRepository)
    : IRequestHandler<CreateWorkflowCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = WorkflowDefinition.Create(
            request.TenantId,
            request.Name,
            request.Category,
            request.CreatedBy,
            request.Description,
            request.TimeoutMinutes);

        await workflowRepository.AddAsync(workflow, cancellationToken);
        return Result.Success(workflow.Id);
    }
}

/// <summary>Handles <see cref="PublishWorkflowCommand"/>.</summary>
public sealed class PublishWorkflowCommandHandler(IWorkflowRepository workflowRepository)
    : IRequestHandler<PublishWorkflowCommand, Result>
{
    public async Task<Result> Handle(PublishWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await workflowRepository.GetByIdAsync(request.WorkflowId, request.TenantId, cancellationToken)
            ?? throw new WorkflowNotFoundException(request.WorkflowId);

        workflow.Publish(request.PublishedBy);
        await workflowRepository.UpdateAsync(workflow, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="UpdateWorkflowDefinitionCommand"/>.</summary>
public sealed class UpdateWorkflowDefinitionCommandHandler(IWorkflowRepository workflowRepository)
    : IRequestHandler<UpdateWorkflowDefinitionCommand, Result>
{
    public async Task<Result> Handle(UpdateWorkflowDefinitionCommand request, CancellationToken cancellationToken)
    {
        var workflow = await workflowRepository.GetByIdAsync(request.WorkflowId, request.TenantId, cancellationToken)
            ?? throw new WorkflowNotFoundException(request.WorkflowId);

        workflow.UpdateDefinition(
            request.DefinitionJson,
            request.UpdatedBy,
            request.Description,
            request.TimeoutMinutes);

        await workflowRepository.UpdateAsync(workflow, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="StartWorkflowCommand"/>.</summary>
public sealed class StartWorkflowCommandHandler(
    IWorkflowRepository workflowRepository,
    IWorkflowExecutionEngine executionEngine)
    : IRequestHandler<StartWorkflowCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(StartWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await workflowRepository.GetByIdAsync(request.WorkflowId, request.TenantId, cancellationToken)
            ?? throw new WorkflowNotFoundException(request.WorkflowId);

        var instance = await executionEngine.StartAsync(
            workflow,
            request.TenantId,
            request.StartedBy,
            request.InputDataJson,
            request.CorrelationId,
            cancellationToken);

        return Result.Success(instance.Id);
    }
}

/// <summary>Handles <see cref="CancelWorkflowInstanceCommand"/>.</summary>
public sealed class CancelWorkflowInstanceCommandHandler(
    IWorkflowExecutionEngine executionEngine)
    : IRequestHandler<CancelWorkflowInstanceCommand, Result>
{
    public async Task<Result> Handle(CancelWorkflowInstanceCommand request, CancellationToken cancellationToken)
    {
        await executionEngine.CancelAsync(request.InstanceId, request.TenantId, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="ResumeWorkflowInstanceCommand"/>.</summary>
public sealed class ResumeWorkflowInstanceCommandHandler(
    IWorkflowExecutionEngine executionEngine)
    : IRequestHandler<ResumeWorkflowInstanceCommand, Result>
{
    public async Task<Result> Handle(ResumeWorkflowInstanceCommand request, CancellationToken cancellationToken)
    {
        await executionEngine.ResumeAsync(request.InstanceId, request.TenantId, request.ResumeDataJson, cancellationToken);
        return Result.Success();
    }
}
