using AiRulesEngine.Web.Models;

namespace AiRulesEngine.Web.Services;

public interface IAiRuleParser
{
    Task<RuleSetDefinition> ParseAsync(string content, CancellationToken cancellationToken);
}
