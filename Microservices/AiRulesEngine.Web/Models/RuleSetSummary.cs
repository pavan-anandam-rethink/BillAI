namespace AiRulesEngine.Web.Models;

public class RuleSetSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int RuleCount { get; set; }
    public int WorkflowRuleCount { get; set; }
}
