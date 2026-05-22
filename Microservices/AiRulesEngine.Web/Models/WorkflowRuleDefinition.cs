namespace AiRulesEngine.Web.Models;

public class WorkflowRuleDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public RuleConditionLogic ConditionLogic { get; set; } = RuleConditionLogic.All;
    public List<RuleCondition> Conditions { get; set; } = new();
    public List<WorkflowActionDefinition> Actions { get; set; } = new();
    public string? Source { get; set; }
}
