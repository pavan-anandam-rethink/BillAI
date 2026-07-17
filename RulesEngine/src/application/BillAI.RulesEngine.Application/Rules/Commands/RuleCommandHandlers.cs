using BillAI.RulesEngine.Application.Interfaces.Repositories;
using BillAI.RulesEngine.Domain.Entities.Rules;
using BillAI.RulesEngine.Domain.Exceptions;
using BillAI.RulesEngine.Shared.Models;
using MediatR;

namespace BillAI.RulesEngine.Application.Rules.Commands;

/// <summary>Handles <see cref="CreateRuleCommand"/>.</summary>
public sealed class CreateRuleCommandHandler(IRuleRepository ruleRepository)
    : IRequestHandler<CreateRuleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateRuleCommand request, CancellationToken cancellationToken)
    {
        if (await ruleRepository.ExistsAsync(request.Name, request.TenantId, cancellationToken))
            return Result.Failure<Guid>($"A rule named '{request.Name}' already exists for this tenant.");

        var rule = RuleDefinition.Create(
            request.TenantId,
            request.Name,
            request.Category,
            request.RuleExpression,
            request.CreatedBy,
            request.Description,
            request.Priority,
            request.Severity,
            request.FailureMessage,
            request.EffectiveFrom,
            request.ExpiresAt);

        await ruleRepository.AddAsync(rule, cancellationToken);
        return Result.Success(rule.Id);
    }
}

/// <summary>Handles <see cref="UpdateRuleCommand"/>.</summary>
public sealed class UpdateRuleCommandHandler(IRuleRepository ruleRepository)
    : IRequestHandler<UpdateRuleCommand, Result>
{
    public async Task<Result> Handle(UpdateRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await ruleRepository.GetByIdAsync(request.RuleId, request.TenantId, cancellationToken)
            ?? throw new RuleNotFoundException(request.RuleId);

        rule.Update(
            request.Name,
            request.Category,
            request.RuleExpression,
            request.UpdatedBy,
            request.Description,
            request.Priority,
            request.Severity,
            request.FailureMessage,
            request.EffectiveFrom,
            request.ExpiresAt,
            request.DecisionTableJson);

        await ruleRepository.UpdateAsync(rule, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="PublishRuleCommand"/>.</summary>
public sealed class PublishRuleCommandHandler(IRuleRepository ruleRepository)
    : IRequestHandler<PublishRuleCommand, Result>
{
    public async Task<Result> Handle(PublishRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await ruleRepository.GetByIdAsync(request.RuleId, request.TenantId, cancellationToken)
            ?? throw new RuleNotFoundException(request.RuleId);

        rule.Publish(request.PublishedBy);
        await ruleRepository.UpdateAsync(rule, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="ArchiveRuleCommand"/>.</summary>
public sealed class ArchiveRuleCommandHandler(IRuleRepository ruleRepository)
    : IRequestHandler<ArchiveRuleCommand, Result>
{
    public async Task<Result> Handle(ArchiveRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await ruleRepository.GetByIdAsync(request.RuleId, request.TenantId, cancellationToken)
            ?? throw new RuleNotFoundException(request.RuleId);

        rule.Archive(request.ArchivedBy);
        await ruleRepository.UpdateAsync(rule, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="DeleteRuleCommand"/>.</summary>
public sealed class DeleteRuleCommandHandler(IRuleRepository ruleRepository)
    : IRequestHandler<DeleteRuleCommand, Result>
{
    public async Task<Result> Handle(DeleteRuleCommand request, CancellationToken cancellationToken)
    {
        await ruleRepository.DeleteAsync(request.RuleId, request.TenantId, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="RollbackRuleCommand"/>.</summary>
public sealed class RollbackRuleCommandHandler(IRuleRepository ruleRepository)
    : IRequestHandler<RollbackRuleCommand, Result>
{
    public async Task<Result> Handle(RollbackRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await ruleRepository.GetByIdAsync(request.RuleId, request.TenantId, cancellationToken)
            ?? throw new RuleNotFoundException(request.RuleId);

        rule.Rollback(request.TargetVersion, request.RolledBackBy);
        await ruleRepository.UpdateAsync(rule, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="ToggleRuleCommand"/>.</summary>
public sealed class ToggleRuleCommandHandler(IRuleRepository ruleRepository)
    : IRequestHandler<ToggleRuleCommand, Result>
{
    public async Task<Result> Handle(ToggleRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await ruleRepository.GetByIdAsync(request.RuleId, request.TenantId, cancellationToken)
            ?? throw new RuleNotFoundException(request.RuleId);

        if (request.Enable)
            rule.Enable(request.UpdatedBy);
        else
            rule.Disable(request.UpdatedBy);

        await ruleRepository.UpdateAsync(rule, cancellationToken);
        return Result.Success();
    }
}
