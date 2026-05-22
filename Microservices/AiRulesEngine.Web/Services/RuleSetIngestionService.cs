using AiRulesEngine.Web.Models;
using Microsoft.AspNetCore.Http;

namespace AiRulesEngine.Web.Services;

public sealed class RuleSetIngestionService
{
    private readonly IFileTextExtractor _textExtractor;
    private readonly IRuleSetParser _parser;
    private readonly IRuleSetStore _store;

    public RuleSetIngestionService(IFileTextExtractor textExtractor, IRuleSetParser parser, IRuleSetStore store)
    {
        _textExtractor = textExtractor;
        _parser = parser;
        _store = store;
    }

    public async Task<RuleSetDefinition> IngestAsync(IFormFile file, string? name, CancellationToken cancellationToken)
    {
        var content = await _textExtractor.ExtractAsync(file, cancellationToken);
        return await IngestTextAsync(content, name, cancellationToken);
    }

    public async Task<RuleSetDefinition> IngestTextAsync(string content, string? name, CancellationToken cancellationToken)
    {
        var ruleSet = await _parser.ParseAsync(content, cancellationToken);

        ruleSet.Rules ??= new List<RuleDefinition>();
        ruleSet.WorkflowRules ??= new List<WorkflowRuleDefinition>();

        if (!string.IsNullOrWhiteSpace(name))
        {
            ruleSet.Name = name;
        }

        if (string.IsNullOrWhiteSpace(ruleSet.Id))
        {
            ruleSet.Id = Guid.NewGuid().ToString("N");
        }

        await _store.SaveAsync(ruleSet, cancellationToken);
        return ruleSet;
    }
}
