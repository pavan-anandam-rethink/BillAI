namespace AiRulesEngine.Web.Models;

public class RuleValidationResponse
{
    public string RuleSetId { get; set; } = string.Empty;
    public List<RuleEvaluationResult> Violations { get; set; } = new();
    public List<WorkflowActionResult> WorkflowActions { get; set; } = new();
}
