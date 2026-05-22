namespace AiRulesEngine.Web.Models;

public class WorkflowActionResult
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; set; }
}
