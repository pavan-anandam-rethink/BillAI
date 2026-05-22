namespace AiRulesEngine.Web.Options;

public class RuleEngineOptions
{
    public string StoragePath { get; set; } = "data/rulesets";
    public string PromptPath { get; set; } = "prompts/rule-parser.txt";
}
