namespace AiRulesEngine.Web.Models;

public class RuleDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public RuleSeverity Severity { get; set; } = RuleSeverity.Error;
    public string Message { get; set; } = string.Empty;
    public RuleConditionLogic ConditionLogic { get; set; } = RuleConditionLogic.All;
    public List<RuleCondition> Conditions { get; set; } = new();
    public RuleConditionLogic AppliesWhenLogic { get; set; } = RuleConditionLogic.All;
    public List<RuleCondition> AppliesWhen { get; set; } = new();
    public string? Source { get; set; }
}
