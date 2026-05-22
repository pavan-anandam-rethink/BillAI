using AiRulesEngine.Web.Models;
using AiRulesEngine.Web.Options;
using Microsoft.Extensions.Options;

namespace AiRulesEngine.Web.Services;

public sealed class CompositeRuleSetParser : IRuleSetParser
{
    private readonly JsonRuleSetParser _jsonParser;
    private readonly IAiRuleParser _aiRuleParser;
    private readonly AiProviderOptions _options;

    public CompositeRuleSetParser(
        JsonRuleSetParser jsonParser,
        IAiRuleParser aiRuleParser,
        IOptions<AiProviderOptions> options)
    {
        _jsonParser = jsonParser;
        _aiRuleParser = aiRuleParser;
        _options = options.Value;
    }

    public async Task<RuleSetDefinition> ParseAsync(string content, CancellationToken cancellationToken)
    {
        if (_jsonParser.TryParse(content, out var ruleSet))
        {
            return ruleSet;
        }

        if (!_options.Enabled)
        {
            throw new InvalidOperationException("Input is not valid JSON and AI parsing is disabled.");
        }

        return await _aiRuleParser.ParseAsync(content, cancellationToken);
    }
}
