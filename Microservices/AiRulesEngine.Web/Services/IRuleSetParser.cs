using AiRulesEngine.Web.Models;

namespace AiRulesEngine.Web.Services;

public interface IRuleSetParser
{
    Task<RuleSetDefinition> ParseAsync(string content, CancellationToken cancellationToken);
}
