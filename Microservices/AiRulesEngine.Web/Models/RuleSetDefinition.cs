namespace AiRulesEngine.Web.Models;

public class RuleSetDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Ruleset";
    public string? Description { get; set; }
    public string Version { get; set; } = "1.0";
    public List<RuleDefinition> Rules { get; set; } = new();
    public List<WorkflowRuleDefinition> WorkflowRules { get; set; } = new();
    public Dictionary<string, string>? Metadata { get; set; }
}
