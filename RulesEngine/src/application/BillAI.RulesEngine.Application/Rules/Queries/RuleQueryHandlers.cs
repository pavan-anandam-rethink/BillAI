using BillAI.RulesEngine.Application.Interfaces.Repositories;
using BillAI.RulesEngine.Domain.Entities.Rules;
using BillAI.RulesEngine.Domain.Exceptions;
using BillAI.RulesEngine.Shared.Models;
using MediatR;

namespace BillAI.RulesEngine.Application.Rules.Queries;

/// <summary>Handles <see cref="GetRuleByIdQuery"/>.</summary>
public sealed class GetRuleByIdQueryHandler(IRuleRepository ruleRepository)
    : IRequestHandler<GetRuleByIdQuery, RuleDefinition?>
{
    public Task<RuleDefinition?> Handle(GetRuleByIdQuery request, CancellationToken cancellationToken)
        => ruleRepository.GetByIdAsync(request.RuleId, request.TenantId, cancellationToken);
}

/// <summary>Handles <see cref="GetRulesQuery"/>.</summary>
public sealed class GetRulesQueryHandler(IRuleRepository ruleRepository)
    : IRequestHandler<GetRulesQuery, PagedResult<RuleDefinition>>
{
    public Task<PagedResult<RuleDefinition>> Handle(GetRulesQuery request, CancellationToken cancellationToken)
        => ruleRepository.GetAllAsync(
            request.TenantId,
            request.Page,
            request.PageSize,
            request.Status,
            request.Category,
            request.SearchTerm,
            cancellationToken);
}

/// <summary>Handles <see cref="GetRuleVersionsQuery"/>.</summary>
public sealed class GetRuleVersionsQueryHandler(IRuleRepository ruleRepository)
    : IRequestHandler<GetRuleVersionsQuery, IReadOnlyList<RuleVersion>>
{
    public async Task<IReadOnlyList<RuleVersion>> Handle(GetRuleVersionsQuery request, CancellationToken cancellationToken)
    {
        var rule = await ruleRepository.GetByIdAsync(request.RuleId, request.TenantId, cancellationToken)
            ?? throw new RuleNotFoundException(request.RuleId);
        return rule.Versions;
    }
}
