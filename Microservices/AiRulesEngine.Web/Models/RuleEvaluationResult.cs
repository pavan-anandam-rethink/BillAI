namespace AiRulesEngine.Web.Models;

public class RuleEvaluationResult
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public RuleSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
}
