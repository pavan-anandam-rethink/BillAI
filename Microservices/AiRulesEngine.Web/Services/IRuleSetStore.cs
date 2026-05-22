using AiRulesEngine.Web.Models;

namespace AiRulesEngine.Web.Services;

public interface IRuleSetStore
{
    Task SaveAsync(RuleSetDefinition ruleSet, CancellationToken cancellationToken);
    Task<RuleSetDefinition?> GetAsync(string ruleSetId, CancellationToken cancellationToken);
}
